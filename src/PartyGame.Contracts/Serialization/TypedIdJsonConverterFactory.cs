using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PartyGame.Contracts.Serialization;

/// <summary>
/// Serializes a typed identifier, such as <c>readonly record struct PlayerId(Guid Value)</c>, as a plain JSON string,
/// both as a value and as a dictionary key. Applied with <c>[JsonConverter(typeof(TypedIdJsonConverterFactory))]</c>
/// on the identifier, so that every serializer handles it the same way without extra configuration.
/// </summary>
/// <remarks>
/// A typed identifier is a struct with a single public property named <c>Value</c>, of type <see cref="Guid"/>
/// or <see cref="string"/>, and a public constructor taking that value.
/// </remarks>
public sealed class TypedIdJsonConverterFactory : JsonConverterFactory
{
    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert) => FindValueProperty(typeToConvert) is not null;

    /// <inheritdoc />
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var valueProperty = FindValueProperty(typeToConvert)
            ?? throw new InvalidOperationException(
                $"{typeToConvert} is not a typed identifier: it needs a single public Value property of type Guid or string, " +
                "and a public constructor taking that value.");

        var create = typeof(TypedIdJsonConverterFactory)
            .GetMethod(nameof(CreateConverter), 1, BindingFlags.NonPublic | BindingFlags.Static, [typeof(PropertyInfo)])!
            .MakeGenericMethod(typeToConvert);
        return (JsonConverter)create.Invoke(null, [valueProperty])!;
    }

    /// <summary>
    /// Returns the <c>Value</c> property of a typed identifier, or <see langword="null"/> if the type is not one.
    /// </summary>
    /// <param name="type">The type to inspect.</param>
    public static PropertyInfo? FindValueProperty(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (!type.IsValueType || type.IsPrimitive || type.IsEnum || type.IsGenericType)
        {
            return null;
        }

        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        if (properties is not [{ Name: "Value", CanRead: true } property]
            || (property.PropertyType != typeof(Guid) && property.PropertyType != typeof(string))
            || type.GetConstructor([property.PropertyType]) is null)
        {
            return null;
        }

        return property;
    }

    private static TypedIdJsonConverter<TId> CreateConverter<TId>(PropertyInfo valueProperty)
        where TId : struct
    {
        var text = Expression.Parameter(typeof(string), "text");
        var value = valueProperty.PropertyType == typeof(Guid)
            ? Expression.Call(typeof(Guid).GetMethod(nameof(Guid.Parse), [typeof(string)])!, text)
            : (Expression)text;
        var parse = Expression.Lambda<Func<string, TId>>(
            Expression.New(typeof(TId).GetConstructor([valueProperty.PropertyType])!, value), text);

        var id = Expression.Parameter(typeof(TId), "id");
        Expression valueText = Expression.Property(id, valueProperty);
        if (valueProperty.PropertyType == typeof(Guid))
        {
            valueText = Expression.Call(valueText, typeof(Guid).GetMethod(nameof(Guid.ToString), [typeof(string)])!, Expression.Constant("D"));
        }

        var format = Expression.Lambda<Func<TId, string>>(valueText, id);

        return new TypedIdJsonConverter<TId>(parse.Compile(), format.Compile());
    }
}
