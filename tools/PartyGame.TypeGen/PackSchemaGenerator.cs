using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using PartyGame.Contracts.Packs;

namespace PartyGame.TypeGen;

/// <summary>
/// Generates the JSON Schema of the pack descriptors, free of any I/O: types in, text out.
/// </summary>
/// <remarks>
/// <see cref="JsonSchemaExporter"/> describes the shape read with <see cref="PackJsonOptions"/>, including
/// <c>additionalProperties: false</c> and the required properties. This generator adds what only attributes say: the
/// <see cref="DescriptionAttribute"/> texts and the simple data annotation constraints. Any other validation attribute
/// fails with a <see cref="TypeGenException"/>, because the editor would silently accept what the server refuses.
/// </remarks>
internal static class PackSchemaGenerator
{
    private const string Dialect = "https://json-schema.org/draft/2020-12/schema";

    private static readonly JsonSerializerOptions _writeOptions = new()
    {
        WriteIndented = true,
        IndentSize = 2,
        NewLine = "\n",

        // The descriptions are French: accents stay readable in the file and in its diffs.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>
    /// Generates the schema of <c>pack.json</c>.
    /// </summary>
    public static string Generate() => Generate(typeof(PackDescriptor));

    /// <summary>
    /// Generates the schema of the given descriptor type, read with <see cref="PackJsonOptions"/>.
    /// </summary>
    public static string Generate(Type rootType)
    {
        ArgumentNullException.ThrowIfNull(rootType);

        var exporterOptions = new JsonSchemaExporterOptions
        {
            TreatNullObliviousAsNonNullable = true,
            TransformSchemaNode = Transform,
        };

        var schema = PackJsonOptions.Default.GetJsonSchemaAsNode(rootType, exporterOptions);
        if (schema is not JsonObject root)
        {
            throw new TypeGenException($"The schema of type '{rootType.FullName}' is not an object.");
        }

        root.Insert(0, "$schema", Dialect);
        return root.ToJsonString(_writeOptions) + "\n";
    }

    private static JsonNode Transform(JsonSchemaExporterContext context, JsonNode node)
    {
        if (node is not JsonObject schema)
        {
            return node;
        }

        var type = context.TypeInfo.Type;
        var attributes = context.PropertyInfo?.AttributeProvider;
        var where = context.PropertyInfo is { } property
            ? $"property '{property.Name}' of type '{property.DeclaringType.FullName}'"
            : $"type '{type.FullName}'";

        // A property is documented by its own description, otherwise by the description of its type.
        var description = Find<DescriptionAttribute>(attributes)?.Description
            ?? type.GetCustomAttribute<DescriptionAttribute>(inherit: false)?.Description;
        if (description is not null)
        {
            // First in the node, where a reader of the file looks for it.
            schema.Insert(0, "description", description);

            // A derived activity: hovering its discriminator value ("type": "quiz") tells what it is.
            if (context.BaseTypeInfo?.PolymorphismOptions?.TypeDiscriminatorPropertyName is { } discriminator
                && schema["properties"]?[discriminator] is JsonObject discriminatorSchema)
            {
                discriminatorSchema.Insert(0, "description", description);
            }
        }

        foreach (var attribute in attributes?.GetCustomAttributes(typeof(ValidationAttribute), inherit: true) ?? [])
        {
            ApplyConstraint(schema, (ValidationAttribute)attribute, type, where);
        }

        return schema;
    }

    private static void ApplyConstraint(JsonObject schema, ValidationAttribute attribute, Type type, string where)
    {
        var isString = type == typeof(string);
        switch (attribute)
        {
            case StringLengthAttribute stringLength when isString:
                if (stringLength.MinimumLength > 0)
                {
                    schema["minLength"] = stringLength.MinimumLength;
                }

                schema["maxLength"] = stringLength.MaximumLength;
                break;

            case LengthAttribute length:
                schema[isString ? "minLength" : "minItems"] = length.MinimumLength;
                schema[isString ? "maxLength" : "maxItems"] = length.MaximumLength;
                break;

            case MinLengthAttribute minLength:
                schema[isString ? "minLength" : "minItems"] = minLength.Length;
                break;

            case MaxLengthAttribute maxLength when maxLength.Length > 0:
                schema[isString ? "maxLength" : "maxItems"] = maxLength.Length;
                break;

            case RangeAttribute range when range.OperandType is { } operand
                && (operand == typeof(int) || operand == typeof(double))
                && !range.MinimumIsExclusive && !range.MaximumIsExclusive:
                var minimum = ToJson(range.Minimum);
                var maximum = ToJson(range.Maximum);
                if (Equals(range.Minimum, range.Maximum))
                {
                    // A single admitted value reads better as a constant, in the schema and in the editor's messages.
                    schema["const"] = minimum;
                }
                else
                {
                    schema["minimum"] = minimum;
                    schema["maximum"] = maximum;
                }

                break;

            default:
                throw new TypeGenException(
                    $"Validation attribute '{attribute.GetType().Name}' on {where} has no JSON Schema translation.");
        }
    }

    private static JsonValue ToJson(object bound) =>
        bound is int integer ? JsonValue.Create(integer) : JsonValue.Create(Convert.ToDouble(bound, CultureInfo.InvariantCulture));

    private static T? Find<T>(ICustomAttributeProvider? provider)
        where T : Attribute =>
        provider?.GetCustomAttributes(typeof(T), inherit: true).OfType<T>().FirstOrDefault();
}
