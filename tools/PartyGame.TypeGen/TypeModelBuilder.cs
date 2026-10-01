using System.Collections.Immutable;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using PartyGame.Contracts.Serialization;
using PartyGame.TypeGen.Model;

namespace PartyGame.TypeGen;

/// <summary>
/// Translates C# types into TypeScript declarations that describe the JSON written with
/// <see cref="ContractJsonOptions"/>. Anything it cannot translate faithfully fails with a
/// <see cref="TypeGenException"/>, rather than degrading to <c>unknown</c>.
/// </summary>
internal sealed class TypeModelBuilder
{
    private static readonly HashSet<Type> _stringTypes = [typeof(string), typeof(Guid), typeof(DateTimeOffset)];

    private static readonly HashSet<Type> _numberTypes =
    [
        typeof(byte), typeof(sbyte), typeof(short), typeof(ushort), typeof(int), typeof(uint),
        typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal),
    ];

    private readonly NullabilityInfoContext _nullability = new();
    private readonly Dictionary<Type, TsDeclaration> _declarations = [];
    private readonly Dictionary<string, Type> _names = new(StringComparer.Ordinal);
    private readonly Queue<Type> _pending = new();

    private TypeModelBuilder()
    {
    }

    /// <summary>
    /// Builds the declarations of the given types and of every type they reference, sorted by name.
    /// </summary>
    public static ImmutableArray<TsDeclaration> Build(IEnumerable<Type> roots)
    {
        var builder = new TypeModelBuilder();
        foreach (var root in roots)
        {
            builder.DeclareRoot(root);
        }

        while (builder._pending.TryDequeue(out var type))
        {
            builder._declarations[type] = builder.CreateDeclaration(type);
        }

        return [.. builder._declarations.Values.OrderBy(d => d.Name, StringComparer.Ordinal)];
    }

    private void DeclareRoot(Type type)
    {
        // A derived type of a polymorphic base is generated with its base, as a member of the union.
        if (FindPolymorphicBase(type) is { } variant)
        {
            Declare(variant.Base);
        }
        else if (IsClientInterface(type))
        {
            // Never the type of a property: only a root, such as IGameClient, describes the messages of the hub.
            Declare(type);
        }
        else
        {
            Reference(type, $"type '{type.FullName}'");
        }
    }

    private TsType Map(Type type, NullabilityInfo nullability, string context)
    {
        if (Nullable.GetUnderlyingType(type) is { } underlying)
        {
            var underlyingNullability = nullability.GenericTypeArguments is [var argument] ? argument : nullability;
            return new TsNullableType(MapNonNull(underlying, underlyingNullability, context));
        }

        var mapped = MapNonNull(type, nullability, context);
        return !type.IsValueType && nullability.ReadState == NullabilityState.Nullable ? new TsNullableType(mapped) : mapped;
    }

    private TsType MapNonNull(Type type, NullabilityInfo nullability, string context)
    {
        if (_stringTypes.Contains(type))
        {
            return TsPrimitiveType.String;
        }

        if (_numberTypes.Contains(type))
        {
            return TsPrimitiveType.Number;
        }

        if (type == typeof(bool))
        {
            return TsPrimitiveType.Boolean;
        }

        if (type == typeof(byte[]))
        {
            throw Unsupported(context, "byte[] is serialized as a base64 string, which the generator does not support");
        }

        if (type.IsArray)
        {
            if (type.GetArrayRank() != 1)
            {
                throw Unsupported(context, $"multidimensional array '{type}' is not supported");
            }

            return new TsArrayType(Map(type.GetElementType()!, nullability.ElementType!, context));
        }

        if ((FindGenericInterface(type, typeof(IReadOnlyDictionary<,>)) ?? FindGenericInterface(type, typeof(IDictionary<,>))) is { } dictionary)
        {
            var keyType = dictionary.GenericTypeArguments[0];
            if (keyType != typeof(string) && !IsTypedId(keyType))
            {
                throw Unsupported(context, $"dictionary key type '{keyType}' is not supported, only strings and typed identifiers are");
            }

            if (!type.IsGenericType || !type.GenericTypeArguments.SequenceEqual(dictionary.GenericTypeArguments))
            {
                throw Unsupported(context, $"dictionary type '{type}' is not supported, use IReadOnlyDictionary or ImmutableDictionary");
            }

            return new TsRecordType(Map(dictionary.GenericTypeArguments[1], nullability.GenericTypeArguments[1], context));
        }

        if (FindGenericInterface(type, typeof(IEnumerable<>)) is { } enumerable)
        {
            if (!type.IsGenericType || !type.GenericTypeArguments.SequenceEqual(enumerable.GenericTypeArguments))
            {
                throw Unsupported(context, $"collection type '{type}' is not supported, use ImmutableArray or IReadOnlyList");
            }

            return new TsArrayType(Map(enumerable.GenericTypeArguments[0], nullability.GenericTypeArguments[0], context));
        }

        return Reference(type, context);
    }

    private TsReferenceType Reference(Type type, string context)
    {
        if (IsFrameworkType(type))
        {
            throw Unsupported(context, $"type '{type}' is not supported");
        }

        if (type.IsGenericType)
        {
            throw Unsupported(context, $"generic type '{type}' is not supported");
        }

        if (IsClientInterface(type))
        {
            throw Unsupported(context, "interfaces are only supported as [JsonPolymorphic] bases, or as hub client interfaces at the root");
        }

        if (FindPolymorphicBase(type) is { } variant)
        {
            // Serialized under its own declared type, a derived type carries no discriminator.
            throw Unsupported(
                context,
                $"'{type.Name}' is a derived type of polymorphic '{variant.Base.Name}', reference '{variant.Base.Name}' instead");
        }

        Declare(type);
        return new TsReferenceType(type.Name);
    }

    private void Declare(Type type)
    {
        if (!_names.TryAdd(type.Name, type))
        {
            var existing = _names[type.Name];
            if (existing != type)
            {
                throw new TypeGenException(
                    $"Types '{existing.FullName}' and '{type.FullName}' would both generate '{type.Name}': rename one of them.");
            }

            return;
        }

        _pending.Enqueue(type);
    }

    private TsDeclaration CreateDeclaration(Type type)
    {
        var context = $"type '{type.FullName}'";

        if (type.IsEnum)
        {
            return CreateStringUnion(type, context);
        }

        if (IsTypedId(type))
        {
            return TypedIdJsonConverterFactory.FindValueProperty(type) is null
                ? throw Unsupported(context, "a typed identifier needs a single public Value property of type Guid or string, and a matching constructor")
                : new TsBrandedString(type.Name, type);
        }

        if (type.IsDefined(typeof(JsonConverterAttribute)))
        {
            throw Unsupported(context, "a custom JSON converter makes the wire format unknown to the generator");
        }

        if (type.IsDefined(typeof(JsonPolymorphicAttribute), inherit: false))
        {
            return CreateUnion(type, context);
        }

        if (IsClientInterface(type))
        {
            return CreateClientInterface(type, context);
        }

        if (type.IsAbstract)
        {
            throw Unsupported(context, "abstract types are only supported as [JsonPolymorphic] bases");
        }

        if (typeof(Delegate).IsAssignableFrom(type) || (type.BaseType is { } baseType && baseType != typeof(object) && baseType != typeof(ValueType) && IsFrameworkType(baseType)))
        {
            throw Unsupported(context, $"types deriving from '{type.BaseType}' are not supported");
        }

        return CreateInterface(type, FindPolymorphicBase(type));
    }

    private static TsStringUnion CreateStringUnion(Type type, string context)
    {
        if (type.IsDefined(typeof(FlagsAttribute)))
        {
            throw Unsupported(context, "[Flags] enums are serialized as comma-separated lists, which the generator does not support");
        }

        var values = type.GetFields(BindingFlags.Public | BindingFlags.Static)
            .OrderBy(f => f.MetadataToken)
            .Select(f => f.GetCustomAttribute<JsonStringEnumMemberNameAttribute>()?.Name ?? f.Name);

        return new TsStringUnion(type.Name, type, [.. values]);
    }

    private TsUnion CreateUnion(Type type, string context)
    {
        if (!type.IsAbstract && !type.IsInterface)
        {
            throw Unsupported(context, "a polymorphic base must be abstract or an interface, otherwise its own instances are serialized without discriminator");
        }

        var derivedTypes = type.GetCustomAttributes<JsonDerivedTypeAttribute>(inherit: false).ToList();
        if (derivedTypes.Count == 0)
        {
            throw Unsupported(context, "a polymorphic base needs at least one [JsonDerivedType]");
        }

        foreach (var derived in derivedTypes)
        {
            if (derived.TypeDiscriminator is null)
            {
                throw Unsupported(context, $"derived type '{derived.DerivedType.Name}' needs a type discriminator");
            }

            if (derived.DerivedType.IsDefined(typeof(JsonPolymorphicAttribute), inherit: false))
            {
                throw Unsupported(context, $"derived type '{derived.DerivedType.Name}' cannot be polymorphic itself");
            }

            if (derived.DerivedType.IsGenericType)
            {
                throw Unsupported(context, $"derived type '{derived.DerivedType}' cannot be generic");
            }

            Declare(derived.DerivedType);
        }

        return new TsUnion(type.Name, type, [.. derivedTypes.Select(d => d.DerivedType.Name).Order(StringComparer.Ordinal)]);
    }

    private TsInterface CreateInterface(Type type, PolymorphicVariant? variant)
    {
        var properties = ImmutableArray.CreateBuilder<TsProperty>();
        var names = new HashSet<string>(StringComparer.Ordinal);

        if (variant is not null)
        {
            properties.Add(new TsProperty(variant.PropertyName, new TsLiteralType(variant.Discriminator)));
            names.Add(variant.PropertyName);
        }

        foreach (var property in GetSerializedProperties(type))
        {
            var context = $"property '{type.Name}.{property.Name}'";

            var ignore = property.GetCustomAttribute<JsonIgnoreAttribute>();
            if (ignore is { Condition: JsonIgnoreCondition.Always })
            {
                continue;
            }

            if (ignore is not null)
            {
                throw Unsupported(context, $"[JsonIgnore(Condition = {ignore.Condition})] makes it optional, but generated properties are always present");
            }

            if (property.IsDefined(typeof(JsonExtensionDataAttribute)) || property.IsDefined(typeof(JsonConverterAttribute)))
            {
                throw Unsupported(context, "[JsonExtensionData] and custom JSON converters make the wire format unknown to the generator");
            }

            var name = property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? JsonNamingPolicy.CamelCase.ConvertName(property.Name);
            if (!names.Add(name))
            {
                throw Unsupported(context, $"its JSON name '{name}' is already used by another property or by the type discriminator");
            }

            properties.Add(new TsProperty(name, Map(property.PropertyType, _nullability.Create(property), context)));
        }

        return new TsInterface(type.Name, type, properties.ToImmutable());
    }

    /// <summary>
    /// Translates a hub client interface: each method is a message whose arguments SignalR serializes one by one,
    /// with the same conventions as any DTO.
    /// </summary>
    private TsClientInterface CreateClientInterface(Type type, string context)
    {
        if (type.IsGenericType)
        {
            throw Unsupported(context, "a hub client interface cannot be generic");
        }

        if (type.GetInterfaces().Length > 0)
        {
            throw Unsupported(context, "a hub client interface cannot extend other interfaces");
        }

        if (type.GetProperties().Length > 0 || type.GetEvents().Length > 0)
        {
            throw Unsupported(context, "a hub client interface only has methods");
        }

        var methods = ImmutableArray.CreateBuilder<TsMethod>();
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance).OrderBy(m => m.MetadataToken))
        {
            var methodContext = $"method '{type.Name}.{method.Name}'";

            if (method.IsGenericMethod)
            {
                throw Unsupported(methodContext, "generic methods are not supported");
            }

            if (method.ReturnType != typeof(Task))
            {
                throw Unsupported(methodContext, "it must return Task, as results from clients are not supported");
            }

            if (!names.Add(method.Name))
            {
                throw Unsupported(methodContext, "overloads are not supported, as SignalR targets a method by its name only");
            }

            var parameters = ImmutableArray.CreateBuilder<TsParameter>();
            foreach (var parameter in method.GetParameters())
            {
                var parameterContext = $"parameter '{type.Name}.{method.Name}({parameter.Name})'";
                if (parameter.ParameterType.IsByRef)
                {
                    throw Unsupported(parameterContext, "ref, in and out parameters are not supported");
                }

                parameters.Add(new TsParameter(parameter.Name!, Map(parameter.ParameterType, _nullability.Create(parameter), parameterContext)));
            }

            methods.Add(new TsMethod(method.Name, parameters.ToImmutable()));
        }

        return new TsClientInterface(type.Name, type, methods.ToImmutable());
    }

    private static IEnumerable<PropertyInfo> GetSerializedProperties(Type type) =>
        type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetMethod is { IsPublic: true } && p.GetIndexParameters().Length == 0)
            .OrderBy(p => Depth(p.DeclaringType!))
            .ThenBy(p => p.MetadataToken);

    private static int Depth(Type type)
    {
        var depth = 0;
        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            depth++;
        }

        return depth;
    }

    private static PolymorphicVariant? FindPolymorphicBase(Type type)
    {
        var candidates = type.GetInterfaces().AsEnumerable();
        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            candidates = candidates.Prepend(current);
        }

        foreach (var candidate in candidates)
        {
            var polymorphic = candidate.GetCustomAttribute<JsonPolymorphicAttribute>(inherit: false);
            if (polymorphic is null)
            {
                continue;
            }

            var derived = candidate.GetCustomAttributes<JsonDerivedTypeAttribute>(inherit: false).FirstOrDefault(d => d.DerivedType == type)
                ?? throw Unsupported(
                    $"type '{type.FullName}'",
                    $"it derives from polymorphic '{candidate.Name}' without being listed in its [JsonDerivedType] attributes");

            // Discriminators are validated when the base is generated.
            return new PolymorphicVariant(candidate, polymorphic.TypeDiscriminatorPropertyName ?? "$type", derived.TypeDiscriminator ?? string.Empty);
        }

        return null;
    }

    private static Type? FindGenericInterface(Type type, Type genericInterface) =>
        (type.IsInterface ? type.GetInterfaces().Prepend(type) : type.GetInterfaces())
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == genericInterface);

    private static bool IsClientInterface(Type type) =>
        type.IsInterface && !type.IsDefined(typeof(JsonPolymorphicAttribute), inherit: false);

    private static bool IsTypedId(Type type) =>
        type.GetCustomAttribute<JsonConverterAttribute>()?.ConverterType == typeof(TypedIdJsonConverterFactory);

    private static bool IsFrameworkType(Type type) =>
        type.Namespace is null
        || type.Namespace == "System" || type.Namespace.StartsWith("System.", StringComparison.Ordinal)
        || type.Namespace == "Microsoft" || type.Namespace.StartsWith("Microsoft.", StringComparison.Ordinal);

    private static TypeGenException Unsupported(string context, string reason) =>
        new($"Cannot generate TypeScript for {context}: {reason}.");

    private sealed record PolymorphicVariant(Type Base, string PropertyName, object Discriminator);
}
