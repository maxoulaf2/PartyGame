using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using PartyGame.Contracts;
using PartyGame.Contracts.Packs;

namespace PartyGame.Content;

/// <summary>
/// Reads the descriptor of a pack and reports every problem at once, where deserializing it stops at the first one.
/// </summary>
/// <remarks>
/// The walk follows the contract that System.Text.Json derives from the descriptor types with
/// <see cref="PackJsonOptions"/>: what it accepts is what the deserializer reads, so the two never drift apart. Each part
/// that can be read is then deserialized on its own, to check its constraints, find its media and hand its activities to
/// their game mode, even when other parts of the descriptor cannot be read.
/// </remarks>
internal sealed class DescriptorReader
{
    private readonly JsonSerializerOptions _options = PackJsonOptions.Default;
    private readonly ImmutableArray<PackProblem>.Builder _problems = ImmutableArray.CreateBuilder<PackProblem>();
    private readonly ImmutableArray<MediaReference>.Builder _media = ImmutableArray.CreateBuilder<MediaReference>();
    private readonly ImmutableArray<RoundReference>.Builder _rounds = ImmutableArray.CreateBuilder<RoundReference>();

    private DescriptorReader()
    {
    }

    /// <summary>
    /// Reads a descriptor that is valid JSON.
    /// </summary>
    /// <param name="root">The root element of <c>pack.json</c>.</param>
    public static DescriptorReading Read(JsonElement root)
    {
        var reader = new DescriptorReader();
        reader.Read(root, typeof(PackDescriptor), JsonPath.Root, nullable: false);
        return new DescriptorReading(reader._problems.ToImmutable(), reader._media.ToImmutable(), reader._rounds.ToImmutable());
    }

    /// <returns>Whether <paramref name="element"/> can be deserialized as <paramref name="type"/>.</returns>
    private bool Read(JsonElement element, Type type, string path, bool nullable)
    {
        if (element.ValueKind == JsonValueKind.Null)
        {
            return nullable || Fail(TypeInvalid(path, type));
        }

        var info = _options.GetTypeInfo(type);
        var readable = info.Kind switch
        {
            JsonTypeInfoKind.Object => ReadObject(element, info, path),
            JsonTypeInfoKind.Enumerable => ReadArray(element, info.ElementType!, path),
            JsonTypeInfoKind.None => ReadValue(element, type, path),
            _ => throw new InvalidOperationException($"Descriptor type {type.Name} is a {info.Kind}, which packs do not use."),
        };

        if (readable && type == typeof(RoundDescriptor))
        {
            _rounds.Add(new RoundReference(path, (RoundDescriptor)Deserialize(element, type)!));
        }

        return readable;
    }

    private bool ReadObject(JsonElement element, JsonTypeInfo info, string path)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return Fail(TypeInvalid(path, info.Type));
        }

        string? discriminator = null;
        if (info.PolymorphismOptions is { } polymorphism)
        {
            discriminator = polymorphism.TypeDiscriminatorPropertyName;
            if (DerivedType(element, polymorphism, path) is not { } derivedType)
            {
                // Without its type, the properties an object may have are unknown: nothing else can be checked.
                return false;
            }

            info = _options.GetTypeInfo(derivedType);
        }

        var properties = info.Properties.ToDictionary(property => property.Name, StringComparer.Ordinal);
        var readable = true;
        foreach (var member in element.EnumerateObject())
        {
            var memberPath = JsonPath.Property(path, member.Name);
            if (member.Name == discriminator)
            {
                continue;
            }

            readable &= properties.TryGetValue(member.Name, out var property)
                ? ReadProperty(member.Value, property, memberPath)
                : Fail(Problems.InDescriptor(PackProblemCode.PackPropertyUnknown, memberPath, ("property", member.Name)));
        }

        foreach (var property in info.Properties.Where(property => property.IsRequired && !element.TryGetProperty(property.Name, out _)))
        {
            readable = Fail(Problems.InDescriptor(PackProblemCode.PackPropertyMissing, path, ("property", property.Name)));
        }

        return readable;
    }

    private Type? DerivedType(JsonElement element, JsonPolymorphismOptions polymorphism, string path)
    {
        var name = polymorphism.TypeDiscriminatorPropertyName;
        if (!element.TryGetProperty(name, out var discriminator))
        {
            Fail(Problems.InDescriptor(PackProblemCode.PackPropertyMissing, path, ("property", name)));
            return null;
        }

        var discriminatorPath = JsonPath.Property(path, name);
        if (discriminator.ValueKind != JsonValueKind.String)
        {
            Fail(TypeInvalid(discriminatorPath, typeof(string)));
            return null;
        }

        var value = discriminator.GetString()!;
        foreach (var derived in polymorphism.DerivedTypes)
        {
            if (derived.TypeDiscriminator is string known && string.Equals(known, value, StringComparison.Ordinal))
            {
                return derived.DerivedType;
            }
        }

        Fail(Problems.InDescriptor(PackProblemCode.PackRoundTypeUnknown, discriminatorPath, ("type", value)));
        return null;
    }

    private bool ReadArray(JsonElement element, Type itemType, string path)
    {
        if (element.ValueKind != JsonValueKind.Array)
        {
            return Fail(Problems.InDescriptor(PackProblemCode.PackValueTypeInvalid, path, ("expected", "array")));
        }

        var readable = true;
        var index = 0;
        foreach (var item in element.EnumerateArray())
        {
            // Unlike properties, collections accept null items whatever the annotations: only a nullable value type does here.
            readable &= Read(item, itemType, JsonPath.Index(path, index++), nullable: Nullable.GetUnderlyingType(itemType) is not null);
        }

        return readable;
    }

    private bool ReadProperty(JsonElement value, JsonPropertyInfo property, string path)
    {
        if (!Read(value, property.PropertyType, path, property.IsSetNullable))
        {
            return false;
        }

        var constraints = property.AttributeProvider?.GetCustomAttributes(typeof(ValidationAttribute), inherit: true) ?? [];
        if (constraints.Length > 0 && value.ValueKind != JsonValueKind.Null)
        {
            var deserialized = Deserialize(value, property.PropertyType);
            foreach (var constraint in constraints.Cast<ValidationAttribute>())
            {
                if (Constraints.Check(constraint, deserialized, path) is { } problem)
                {
                    _problems.Add(problem);
                }
            }
        }

        // A value out of its bounds is still read: the parts around it are checked as usual.
        return true;
    }

    private bool ReadValue(JsonElement element, Type type, string path)
    {
        var readable = Expected(type) switch
        {
            "boolean" => element.ValueKind is JsonValueKind.True or JsonValueKind.False,
            "integer" or "number" => element.ValueKind == JsonValueKind.Number,
            _ => element.ValueKind == JsonValueKind.String,
        };

        object? value = null;
        try
        {
            value = readable ? Deserialize(element, type) : null;
        }
        catch (JsonException)
        {
            // Right kind, wrong value: a decimal or an overflow for an integer, for example.
            readable = false;
        }

        if (!readable)
        {
            return Fail(TypeInvalid(path, type));
        }

        if (value is MediaPath media)
        {
            _media.Add(new MediaReference(path, media));
        }

        return true;
    }

    private PackProblem TypeInvalid(string path, Type type) =>
        Problems.InDescriptor(PackProblemCode.PackValueTypeInvalid, path, ("expected", Expected(type)));

    // The JSON type a value of this type is written as, named as in JSON Schema.
    private string Expected(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;
        if (underlying.IsEnum)
        {
            return "string";
        }

        return _options.GetTypeInfo(underlying).Kind switch
        {
            JsonTypeInfoKind.Object => "object",
            JsonTypeInfoKind.Enumerable => "array",
            _ => Type.GetTypeCode(underlying) switch
            {
                TypeCode.Boolean => "boolean",
                TypeCode.SByte or TypeCode.Byte or TypeCode.Int16 or TypeCode.UInt16 or TypeCode.Int32 or TypeCode.UInt32
                    or TypeCode.Int64 or TypeCode.UInt64 => "integer",
                TypeCode.Single or TypeCode.Double or TypeCode.Decimal => "number",
                _ => "string",
            },
        };
    }

    private object? Deserialize(JsonElement element, Type type) => element.Deserialize(type, _options);

    private bool Fail(PackProblem problem)
    {
        _problems.Add(problem);
        return false;
    }
}
