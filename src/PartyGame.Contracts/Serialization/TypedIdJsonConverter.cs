using System.Text.Json;
using System.Text.Json.Serialization;

namespace PartyGame.Contracts.Serialization;

/// <summary>
/// Converts a typed identifier to and from a JSON string. Created by <see cref="TypedIdJsonConverterFactory"/>.
/// </summary>
internal sealed class TypedIdJsonConverter<TId>(Func<string, TId> parse, Func<TId, string> format) : JsonConverter<TId>
    where TId : struct
{
    public override TId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException($"Expected a string for {typeof(TId).Name}, got {reader.TokenType}.");
        }

        return Parse(reader.GetString()!);
    }

    public override void Write(Utf8JsonWriter writer, TId value, JsonSerializerOptions options) =>
        writer.WriteStringValue(format(value));

    public override TId ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        Parse(reader.GetString()!);

    public override void WriteAsPropertyName(Utf8JsonWriter writer, TId value, JsonSerializerOptions options) =>
        writer.WritePropertyName(format(value));

    private TId Parse(string text)
    {
        try
        {
            return parse(text);
        }
        catch (FormatException ex)
        {
            throw new JsonException($"'{text}' is not a valid {typeof(TId).Name}.", ex);
        }
    }
}
