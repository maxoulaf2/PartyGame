using System.Text.Json;
using System.Text.Json.Serialization;

namespace PartyGame.Contracts.Packs;

/// <summary>
/// How <c>pack.json</c> files are read, and how their schema is generated. The reading is strict, so that a typo is
/// never silently ignored.
/// </summary>
public static class PackJsonOptions
{
    /// <summary>
    /// Read-only options for pack descriptors.
    /// </summary>
    public static JsonSerializerOptions Default { get; } = CreateDefault();

    private static JsonSerializerOptions CreateDefault()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,

            // A misspelled property is an error, never ignored.
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,

            // Standard JSON only: what the editor accepts without a comment or a trailing comma is what the server reads.
            ReadCommentHandling = JsonCommentHandling.Disallow,
            AllowTrailingCommas = false,

            // An absent or null non-nullable property is an error.
            RespectNullableAnnotations = true,

            // Authors write the "type" of an activity wherever they like, not necessarily first.
            AllowOutOfOrderMetadataProperties = true,
        };

        // Enum members are written as named in C#, as on the wire.
        options.Converters.Add(new JsonStringEnumConverter(namingPolicy: null, allowIntegerValues: false));
        options.MakeReadOnly(populateMissingResolver: true);
        return options;
    }
}
