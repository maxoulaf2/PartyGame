using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using PartyGame.Contracts.Packs;

namespace PartyGame.Contracts.Tests.Packs;

public sealed class PackDescriptorTests
{
    private const string Envelope = """
        {
          "$schema": "../../schemas/pack.schema.json",
          "formatVersion": 1,
          "title": "Soirée quiz",
          "description": "Culture générale",
          "rounds": []
        }
        """;

    [Fact]
    public void Deserialize_Envelope_ReadsEveryProperty()
    {
        var pack = Read(Envelope);

        Assert.Equal("../../schemas/pack.schema.json", pack.Schema);
        Assert.Equal(1, pack.FormatVersion);
        Assert.Equal("Soirée quiz", pack.Title);
        Assert.Equal("Culture générale", pack.Description);
        Assert.Empty(pack.Rounds);
    }

    [Fact]
    public void Deserialize_EnvelopeWithoutOptionalProperties_LeavesThemNull()
    {
        var pack = Read("""{"formatVersion":1,"title":"Soirée quiz","rounds":[]}""");

        Assert.Null(pack.Schema);
        Assert.Null(pack.Description);
    }

    [Theory]
    [InlineData("""{"type":"quiz","title":"Manche 1"}""")]
    [InlineData("""{"title":"Manche 1","type":"quiz"}""")]
    public void Deserialize_RoundWithTypeAnywhere_ReadsItsMode(string round)
    {
        var pack = Read($$"""{"formatVersion":1,"title":"Soirée quiz","rounds":[{{round}}]}""");

        var quiz = Assert.IsType<QuizRoundDescriptor>(Assert.Single(pack.Rounds));
        Assert.Equal("Manche 1", quiz.Title);
    }

    [Theory]
    [InlineData("""{"formatVersion":1,"titel":"Soirée quiz","title":"Soirée quiz","rounds":[]}""")]
    [InlineData("""{"formatVersion":1,"title":"Soirée quiz","rounds":[{"type":"quiz","title":"Manche 1","titel":"x"}]}""")]
    public void Deserialize_UnknownProperty_Throws(string json) =>
        Assert.Throws<JsonException>(() => Read(json));

    [Fact]
    public void Deserialize_UnknownRoundType_Throws() =>
        Assert.Throws<JsonException>(() => Read("""{"formatVersion":1,"title":"Soirée quiz","rounds":[{"type":"karaoke","title":"Manche 1"}]}"""));

    [Fact]
    public void Deserialize_RoundWithoutType_Throws() =>
        // System.Text.Json reports a missing discriminator as unsupported rather than as malformed JSON.
        Assert.Throws<NotSupportedException>(() => Read("""{"formatVersion":1,"title":"Soirée quiz","rounds":[{"title":"Manche 1"}]}"""));

    [Theory]
    [InlineData("""{"title":"Soirée quiz","rounds":[]}""")]
    [InlineData("""{"formatVersion":1,"rounds":[]}""")]
    [InlineData("""{"formatVersion":1,"title":"Soirée quiz"}""")]
    [InlineData("""{"formatVersion":1,"title":null,"rounds":[]}""")]
    [InlineData("""{"formatVersion":"1","title":"Soirée quiz","rounds":[]}""")]
    [InlineData("""{"formatVersion":1,"title":"Soirée quiz","rounds":[{"type":"quiz"}]}""")]
    public void Deserialize_MissingOrMistypedProperty_Throws(string json) =>
        Assert.Throws<JsonException>(() => Read(json));

    [Theory]
    [InlineData("""{"formatVersion":1,"title":"Soirée quiz","rounds":[],}""")]
    [InlineData("""{"formatVersion":1,"title":"Soirée quiz",/* comment */"rounds":[]}""")]
    [InlineData("""{"formatVersion":1,"title":"Soirée quiz","rounds":[]} // comment""")]
    public void Deserialize_NonStandardJson_Throws(string json) =>
        Assert.Throws<JsonException>(() => Read(json));

    [Fact]
    public void Validate_ValidEnvelope_ReportsNothing() =>
        Assert.Empty(Validate(Read(Envelope) with { Rounds = [new QuizRoundDescriptor { Title = "Manche 1" }] }));

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void Validate_UnsupportedFormatVersion_ReportsFormatVersion(int version) =>
        Assert.Equal([nameof(PackDescriptor.FormatVersion)], Validate(ValidPack() with { FormatVersion = version }));

    [Theory]
    [InlineData(0)]
    [InlineData(61)]
    public void Validate_TitleLengthOutOfBounds_ReportsTitle(int length) =>
        Assert.Equal([nameof(PackDescriptor.Title)], Validate(ValidPack() with { Title = new string('a', length) }));

    [Fact]
    public void Validate_DescriptionTooLong_ReportsDescription() =>
        Assert.Equal([nameof(PackDescriptor.Description)], Validate(ValidPack() with { Description = new string('a', 201) }));

    [Fact]
    public void Validate_NoRound_ReportsRounds() =>
        Assert.Equal([nameof(PackDescriptor.Rounds)], Validate(ValidPack() with { Rounds = [] }));

    [Theory]
    [InlineData(0)]
    [InlineData(61)]
    public void Validate_RoundTitleLengthOutOfBounds_ReportsTitle(int length) =>
        Assert.Equal([nameof(RoundDescriptor.Title)], Validate(new QuizRoundDescriptor { Title = new string('a', length) }));

    private static PackDescriptor Read(string json) =>
        JsonSerializer.Deserialize<PackDescriptor>(json, PackJsonOptions.Default)
        ?? throw new InvalidOperationException("The descriptor is null.");

    private static PackDescriptor ValidPack() => new()
    {
        FormatVersion = PackDescriptor.CurrentFormatVersion,
        Title = "Soirée quiz",
        Rounds = [new QuizRoundDescriptor { Title = "Manche 1" }],
    };

    // The same attributes produce the schema constraints. The validator checks one object, not the objects it contains.
    private static List<string> Validate(object instance)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(instance, new ValidationContext(instance), results, validateAllProperties: true);
        return [.. results.SelectMany(r => r.MemberNames)];
    }
}
