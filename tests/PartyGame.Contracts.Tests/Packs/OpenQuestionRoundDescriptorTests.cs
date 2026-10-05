using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using PartyGame.Contracts.Packs;

namespace PartyGame.Contracts.Tests.Packs;

public sealed class OpenQuestionRoundDescriptorTests
{
    [Fact]
    public void Deserialize_CompleteOpenQuestionRound_ReadsEveryProperty()
    {
        var round = ReadRound("""
            {"type":"openquestion","title":"Réponses libres","answerSeconds":45,"points":500,"speedBonus":200,"maxLength":60,
             "questions":[{"text":"Qui a peint La Joconde ?","image":"images/joconde.jpg","answerSeconds":20,"answer":"Léonard de Vinci",
             "acceptedAnswers":["Vinci","Leonardo da Vinci"],"inputMode":"numeric"}]}
            """);

        Assert.Equal("Réponses libres", round.Title);
        Assert.Equal(45, round.AnswerSeconds);
        Assert.Equal(500, round.Points);
        Assert.Equal(200, round.SpeedBonus);
        Assert.Equal(60, round.MaxLength);
        var question = Assert.Single(round.Questions);
        Assert.Equal("Qui a peint La Joconde ?", question.Text);
        Assert.Equal(new MediaPath("images/joconde.jpg"), question.Image);
        Assert.Equal(20, question.AnswerSeconds);
        Assert.Equal("Léonard de Vinci", question.Answer);
        Assert.Equal(["Vinci", "Leonardo da Vinci"], question.AcceptedAnswers);
        Assert.Equal(OpenQuestionInputMode.Numeric, question.InputMode);
    }

    [Fact]
    public void Deserialize_OpenQuestionRoundWithoutOptionalProperties_UsesDefaults()
    {
        var round = ReadRound("""{"type":"openquestion","title":"Manche 1","questions":[{"text":"Question ?","answer":"Oui"}]}""");

        Assert.Equal(30, round.AnswerSeconds);
        Assert.Equal(1000, round.Points);
        Assert.Equal(0, round.SpeedBonus);
        Assert.Equal(40, round.MaxLength);
        var question = Assert.Single(round.Questions);
        Assert.Null(question.Image);
        Assert.Null(question.AnswerSeconds);
        Assert.Empty(question.AcceptedAnswers);
        Assert.Equal(OpenQuestionInputMode.Text, question.InputMode);
    }

    [Theory]
    [InlineData("""{"type":"openquestion","title":"Manche 1"}""")]
    [InlineData("""{"type":"openquestion","title":"Manche 1","questions":[{"text":"Question ?"}]}""")]
    [InlineData("""{"type":"openquestion","title":"Manche 1","questions":[{"answer":"Oui"}]}""")]
    [InlineData("""{"type":"openquestion","title":"Manche 1","questions":[{"text":"Question ?","answer":"Oui","inputMode":"Numeric"}]}""")]
    [InlineData("""{"type":"openquestion","title":"Manche 1","questions":[{"text":"Question ?","answer":"Oui","acceptedAnswers":"Non"}]}""")]
    [InlineData("""{"type":"openquestion","title":"Manche 1","questions":[{"text":"Question ?","answer":"Oui","choices":[]}]}""")]
    public void Deserialize_MissingMistypedOrUnknownProperty_Throws(string round) =>
        Assert.Throws<JsonException>(() => ReadRound(round));

    [Fact]
    public void Validate_ValidRoundAndQuestion_ReportNothing()
    {
        Assert.Empty(Validate(Round()));
        Assert.Empty(Validate(Question()));
    }

    [Theory]
    [InlineData(nameof(OpenQuestionRoundDescriptor.AnswerSeconds), 4)]
    [InlineData(nameof(OpenQuestionRoundDescriptor.AnswerSeconds), 121)]
    [InlineData(nameof(OpenQuestionRoundDescriptor.Points), -1)]
    [InlineData(nameof(OpenQuestionRoundDescriptor.Points), 10_001)]
    [InlineData(nameof(OpenQuestionRoundDescriptor.SpeedBonus), -1)]
    [InlineData(nameof(OpenQuestionRoundDescriptor.SpeedBonus), 10_001)]
    [InlineData(nameof(OpenQuestionRoundDescriptor.MaxLength), 4)]
    [InlineData(nameof(OpenQuestionRoundDescriptor.MaxLength), 101)]
    public void Validate_RoundValueOutOfBounds_ReportsIt(string property, int value)
    {
        var round = property switch
        {
            nameof(OpenQuestionRoundDescriptor.AnswerSeconds) => Round() with { AnswerSeconds = value },
            nameof(OpenQuestionRoundDescriptor.Points) => Round() with { Points = value },
            nameof(OpenQuestionRoundDescriptor.SpeedBonus) => Round() with { SpeedBonus = value },
            _ => Round() with { MaxLength = value },
        };

        Assert.Equal([property], Validate(round));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(51)]
    public void Validate_QuestionCountOutOfBounds_ReportsQuestions(int count) =>
        Assert.Equal([nameof(OpenQuestionRoundDescriptor.Questions)], Validate(Round() with { Questions = [.. Enumerable.Repeat(Question(), count)] }));

    [Theory]
    [InlineData(0)]
    [InlineData(201)]
    public void Validate_QuestionTextLengthOutOfBounds_ReportsText(int length) =>
        Assert.Equal([nameof(OpenQuestionDescriptor.Text)], Validate(Question() with { Text = new string('a', length) }));

    [Theory]
    [InlineData(4)]
    [InlineData(121)]
    public void Validate_QuestionAnswerSecondsOutOfBounds_ReportsAnswerSeconds(int seconds) =>
        Assert.Equal([nameof(OpenQuestionDescriptor.AnswerSeconds)], Validate(Question() with { AnswerSeconds = seconds }));

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Validate_AnswerLengthOutOfBounds_ReportsAnswer(int length) =>
        Assert.Equal([nameof(OpenQuestionDescriptor.Answer)], Validate(Question() with { Answer = new string('a', length) }));

    [Fact]
    public void Validate_TooManyAcceptedAnswers_ReportsAcceptedAnswers() =>
        Assert.Equal([nameof(OpenQuestionDescriptor.AcceptedAnswers)], Validate(Question() with { AcceptedAnswers = [.. Enumerable.Range(0, 21).Select(i => $"Réponse {i}")] }));

    private static OpenQuestionRoundDescriptor ReadRound(string round) =>
        Assert.IsType<OpenQuestionRoundDescriptor>(Assert.Single(
            (JsonSerializer.Deserialize<PackDescriptor>($$"""{"formatVersion":1,"title":"Soirée","rounds":[{{round}}]}""", PackJsonOptions.Default)
             ?? throw new InvalidOperationException("The descriptor is null.")).Rounds));

    private static OpenQuestionRoundDescriptor Round() => new() { Title = "Manche 1", Questions = [Question()] };

    private static OpenQuestionDescriptor Question() => new() { Text = "Capitale de la France ?", Answer = "Paris" };

    // The same attributes produce the schema constraints. The validator checks one object, not the objects it contains.
    private static List<string> Validate(object instance)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(instance, new ValidationContext(instance), results, validateAllProperties: true);
        return [.. results.SelectMany(r => r.MemberNames)];
    }
}
