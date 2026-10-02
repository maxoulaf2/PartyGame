using System.Collections.Immutable;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using PartyGame.Contracts.Serialization;

namespace PartyGame.TypeGen.Tests.Fixtures;

/// <summary>
/// One of each construction the pack schema generator supports, as they will appear in PartyGame.Contracts.Packs.
/// </summary>
public static class PackSchemaFixtures
{
    [Description("Un descripteur.")]
    public sealed record SampleDescriptor
    {
        [StringLength(60, MinimumLength = 1)]
        [Description("Le titre.")]
        public required string Title { get; init; }

        [StringLength(200)]
        public string? Summary { get; init; }

        [Length(2, 4)]
        public required string Code { get; init; }

        [Range(1, 1)]
        public required int Version { get; init; }

        [Range(5, 120)]
        public int? Seconds { get; init; }

        [Range(0.5, 2.5)]
        public double Ratio { get; init; }

        [MinLength(1)]
        public required ImmutableArray<SampleActivity> Activities { get; init; }

        [Length(2, 4)]
        public ImmutableArray<string> Tags { get; init; } = [];

        [MaxLength(3)]
        public ImmutableArray<int> Scores { get; init; } = [];

        public SampleSettings? Settings { get; init; }

        [Description("Réglages propres à ce descripteur.")]
        public SampleSettings? Overrides { get; init; }

        public SamplePath Cover { get; init; }

        [Description("Une image.")]
        public SamplePath? Picture { get; init; }
    }

    [JsonConverter(typeof(TypedIdJsonConverterFactory))]
    public readonly record struct SamplePath(string Value);

    [Description("Des réglages.")]
    public sealed record SampleSettings
    {
        public bool Shuffle { get; init; }
    }

    [JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
    [JsonDerivedType(typeof(SampleQuiz), "quiz")]
    [JsonDerivedType(typeof(SampleBlindTest), "blindTest")]
    [Description("Une activité.")]
    public abstract record SampleActivity
    {
        public required string Title { get; init; }
    }

    [Description("Un quiz.")]
    public sealed record SampleQuiz : SampleActivity
    {
        public int Points { get; init; }
    }

    public sealed record SampleBlindTest : SampleActivity;

    public sealed record WithRequired
    {
        [Required]
        public string? Title { get; init; }
    }

    public sealed record WithExclusiveRange
    {
        [Range(0, 10, MinimumIsExclusive = true)]
        public int Count { get; init; }
    }

    public sealed record WithStringLengthOnNumber
    {
        [StringLength(3)]
        public int Count { get; init; }
    }
}
