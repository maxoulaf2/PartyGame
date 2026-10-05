using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using PartyGame.Contracts.Packs;

namespace PartyGame.Contracts.Tests.Packs;

public sealed class BuzzerRoundDescriptorTests
{
    [Fact]
    public void Deserialize_CompleteBuzzerRound_ReadsEveryProperty()
    {
        var buzzer = ReadRound("""{"type":"buzzer","title":"Le plus rapide","points":500,"questions":[{"text":"Quel pays a ce drapeau ?","answer":"L'Italie","image":"images/drapeau.png"}]}""");

        Assert.Equal("Le plus rapide", buzzer.Title);
        Assert.Equal(500, buzzer.Points);
        var question = Assert.Single(buzzer.Questions);
        Assert.Equal("Quel pays a ce drapeau ?", question.Text);
        Assert.Equal("L'Italie", question.Answer);
        Assert.Equal(new MediaPath("images/drapeau.png"), question.Image);
    }

    [Fact]
    public void Deserialize_BuzzerRoundWithoutOptionalProperties_UsesDefaults()
    {
        var buzzer = ReadRound("""{"type":"buzzer","title":"Manche 1","questions":[{"text":"Question ?","answer":"Oui"}]}""");

        Assert.Equal(1000, buzzer.Points);
        Assert.Null(Assert.Single(buzzer.Questions).Image);
    }

    [Theory]
    [InlineData("""{"type":"buzzer","title":"Manche 1"}""")]
    [InlineData("""{"type":"buzzer","title":"Manche 1","questions":[{"text":"Question ?"}]}""")]
    [InlineData("""{"type":"buzzer","title":"Manche 1","questions":[{"answer":"Oui"}]}""")]
    [InlineData("""{"type":"buzzer","title":"Manche 1","questions":[],"points":"1000"}""")]
    [InlineData("""{"type":"buzzer","title":"Manche 1","questions":[{"text":"Question ?","answer":"Oui","image":42}]}""")]
    public void Deserialize_MissingOrMistypedProperty_Throws(string round) =>
        Assert.Throws<JsonException>(() => ReadRound(round));

    [Theory]
    [InlineData("""{"type":"buzzer","title":"Manche 1","questions":[],"answerSeconds":30}""")]
    [InlineData("""{"type":"buzzer","title":"Manche 1","questions":[{"text":"Question ?","answer":"Oui","choices":[]}]}""")]
    public void Deserialize_UnknownProperty_Throws(string round) =>
        Assert.Throws<JsonException>(() => ReadRound(round));

    [Fact]
    public void Validate_ValidRoundAndQuestion_ReportNothing()
    {
        Assert.Empty(Validate(Round()));
        Assert.Empty(Validate(Question()));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(10_001)]
    public void Validate_PointsOutOfBounds_ReportsPoints(int points) =>
        Assert.Equal([nameof(BuzzerRoundDescriptor.Points)], Validate(Round() with { Points = points }));

    [Theory]
    [InlineData(0)]
    [InlineData(51)]
    public void Validate_QuestionCountOutOfBounds_ReportsQuestions(int count) =>
        Assert.Equal([nameof(BuzzerRoundDescriptor.Questions)], Validate(Round() with { Questions = [.. Enumerable.Repeat(Question(), count)] }));

    [Theory]
    [InlineData(0)]
    [InlineData(201)]
    public void Validate_QuestionTextLengthOutOfBounds_ReportsText(int length) =>
        Assert.Equal([nameof(BuzzerQuestion.Text)], Validate(Question() with { Text = new string('a', length) }));

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Validate_AnswerLengthOutOfBounds_ReportsAnswer(int length) =>
        Assert.Equal([nameof(BuzzerQuestion.Answer)], Validate(Question() with { Answer = new string('a', length) }));

    private static BuzzerRoundDescriptor ReadRound(string round) =>
        Assert.IsType<BuzzerRoundDescriptor>(Assert.Single(
            (JsonSerializer.Deserialize<PackDescriptor>($$"""{"formatVersion":1,"title":"Soirée","rounds":[{{round}}]}""", PackJsonOptions.Default)
             ?? throw new InvalidOperationException("The descriptor is null.")).Rounds));

    private static BuzzerRoundDescriptor Round() => new() { Title = "Manche 1", Questions = [Question()] };

    private static BuzzerQuestion Question() => new() { Text = "Capitale de la France ?", Answer = "Paris" };

    // The same attributes produce the schema constraints. The validator checks one object, not the objects it contains.
    private static List<string> Validate(object instance)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(instance, new ValidationContext(instance), results, validateAllProperties: true);
        return [.. results.SelectMany(r => r.MemberNames)];
    }
}
