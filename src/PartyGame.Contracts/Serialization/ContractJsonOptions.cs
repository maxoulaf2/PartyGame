using System.Text.Json;
using System.Text.Json.Serialization;

namespace PartyGame.Contracts.Serialization;

/// <summary>
/// JSON conventions of the wire, shared by every serializer that talks to the clients.
/// The TypeScript types generated from this assembly assume exactly these conventions.
/// </summary>
public static class ContractJsonOptions
{
    /// <summary>
    /// Read-only options with the wire conventions applied.
    /// </summary>
    public static JsonSerializerOptions Default { get; } = CreateDefault();

    /// <summary>
    /// Applies the wire conventions to existing options, such as those of SignalR or minimal APIs.
    /// </summary>
    /// <param name="options">The options to configure.</param>
    public static void Apply(JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;

        // Dictionary keys are data (identifiers, nicknames), never renamed.
        options.DictionaryKeyPolicy = null;

        // Nullable properties are always present, so that TypeScript can type them as T | null.
        options.DefaultIgnoreCondition = JsonIgnoreCondition.Never;

        // Non-nullable properties are never null on the wire, in either direction.
        options.RespectNullableAnnotations = true;
        options.RespectRequiredConstructorParameters = true;

        // The "type" of a polymorphic message may come anywhere in its object, wherever the client code put it.
        options.AllowOutOfOrderMetadataProperties = true;

        // Enum members are written as named in C#, which is what the generated string unions contain.
        options.Converters.Add(new JsonStringEnumConverter(namingPolicy: null, allowIntegerValues: false));
    }

    private static JsonSerializerOptions CreateDefault()
    {
        var options = new JsonSerializerOptions();
        Apply(options);
        options.MakeReadOnly(populateMissingResolver: true);
        return options;
    }
}
