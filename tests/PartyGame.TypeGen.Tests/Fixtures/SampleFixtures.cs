using System.Collections.Immutable;
using System.Text.Json.Serialization;
using PartyGame.Contracts.Serialization;

namespace PartyGame.TypeGen.Tests.Fixtures;

/// <summary>
/// One of each construction the generator supports, as they will appear in PartyGame.Contracts.
/// </summary>
public static class SampleFixtures
{
    [JsonConverter(typeof(TypedIdJsonConverterFactory))]
    public readonly record struct SampleId(Guid Value);

    [JsonConverter(typeof(TypedIdJsonConverterFactory))]
    public readonly record struct SampleCode(string Value);

    public enum SampleColor
    {
        Red,
        Green,
        [JsonStringEnumMemberName("deep-blue")]
        Blue,
    }

    public sealed record SampleItem(string Label, int? Rank);

    [JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
    [JsonDerivedType(typeof(SampleSquare), "square")]
    [JsonDerivedType(typeof(SampleCircle), "circle")]
    public abstract record SampleShape(string Name);

    public sealed record SampleCircle(string Name, double Radius) : SampleShape(Name);

    public sealed record SampleSquare(string Name, long Side) : SampleShape(Name);

    public sealed record SampleDto(
        SampleId Id,
        SampleCode? Code,
        SampleColor Color,
        SampleColor? Accent,
        string? Nickname,
        int Count,
        bool Ready,
        DateTimeOffset CreatedAt,
        ImmutableArray<SampleItem> Items,
        IReadOnlyList<string?> Tags,
        ImmutableArray<ImmutableArray<int>> Grid,
        ImmutableDictionary<SampleId, int> Scores,
        IReadOnlyDictionary<string, SampleItem?> ByName,
        SampleShape Shape,
        SampleShape? OptionalShape,
        [property: JsonPropertyName("custom_name")] string Renamed)
    {
        [JsonIgnore]
        public string Hidden => Renamed.ToUpperInvariant();
    }
}
