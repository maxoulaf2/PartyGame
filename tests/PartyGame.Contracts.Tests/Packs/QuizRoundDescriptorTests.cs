using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using PartyGame.Contracts.Packs;

namespace PartyGame.Contracts.Tests.Packs;

public sealed class QuizRoundDescriptorTests
{
    private const string CompletePack = """
        {
          "formatVersion": 1,
          "title": "Soirée quiz",
          "rounds": [
            {
              "type": "quiz",
              "title": "Culture générale",
              "answerSeconds": 30,
              "points": 500,
              "speedBonus": 250,
              "shuffleChoices": true,
              "questions": [
                {
                  "text": "Quel pays a ce drapeau ?",
                  "image": "images/drapeau.png",
                  "answerSeconds": 45,
                  "choices": [
                    { "text": "Italie" },
                    { "text": "France", "correct": true },
                    { "text": "Irlande", "correct": false }
                  ]
                }
              ]
            }
          ]
        }
        """;

    [Fact]
    public void Deserialize_CompleteQuizRound_ReadsEveryProperty()
    {
        var pack = Read(CompletePack);

        var quiz = Assert.IsType<QuizRoundDescriptor>(Assert.Single(pack.Rounds));
        Assert.Equal("Culture générale", quiz.Title);
        Assert.Equal(30, quiz.AnswerSeconds);
        Assert.Equal(500, quiz.Points);
        Assert.Equal(250, quiz.SpeedBonus);
        Assert.True(quiz.ShuffleChoices);

        var question = Assert.Single(quiz.Questions);
        Assert.Equal("Quel pays a ce drapeau ?", question.Text);
        Assert.Equal(new MediaPath("images/drapeau.png"), question.Image);
        Assert.Equal(45, question.AnswerSeconds);
        Assert.Equal(
            [("Italie", false), ("France", true), ("Irlande", false)],
            question.Choices.Select(choice => (choice.Text, choice.Correct)));
    }

    [Fact]
    public void Deserialize_QuizRoundWithoutOptionalProperties_UsesDefaults()
    {
        var quiz = ReadRound("""{"type":"quiz","title":"Manche 1","questions":[{"text":"Question ?","choices":[{"text":"Oui","correct":true},{"text":"Non"}]}]}""");

        Assert.Equal(QuizRoundDescriptor.DefaultAnswerSeconds, quiz.AnswerSeconds);
        Assert.Equal(20, quiz.AnswerSeconds);
        Assert.Equal(1000, quiz.Points);
        Assert.Equal(0, quiz.SpeedBonus);
        Assert.False(quiz.ShuffleChoices);

        var question = Assert.Single(quiz.Questions);
        Assert.Null(question.Image);
        Assert.Null(question.AnswerSeconds);
        Assert.False(question.Choices[1].Correct);
    }

    [Theory]
    [InlineData("""{"type":"quiz","title":"Manche 1"}""")]
    [InlineData("""{"type":"quiz","title":"Manche 1","questions":null}""")]
    [InlineData("""{"type":"quiz","title":"Manche 1","questions":[{"choices":[]}]}""")]
    [InlineData("""{"type":"quiz","title":"Manche 1","questions":[{"text":"Question ?"}]}""")]
    [InlineData("""{"type":"quiz","title":"Manche 1","questions":[{"text":"Question ?","choices":[{"correct":true}]}]}""")]
    [InlineData("""{"type":"quiz","title":"Manche 1","questions":[],"shuffleChoices":null}""")]
    [InlineData("""{"type":"quiz","title":"Manche 1","questions":[],"points":"1000"}""")]
    [InlineData("""{"type":"quiz","title":"Manche 1","questions":[{"text":"Question ?","image":42,"choices":[]}]}""")]
    [InlineData("""{"type":"quiz","title":"Manche 1","questions":[{"text":"Question ?","choices":[{"text":"Oui","correct":"true"}]}]}""")]
    public void Deserialize_MissingOrMistypedProperty_Throws(string round) =>
        Assert.Throws<JsonException>(() => ReadRound(round));

    [Theory]
    [InlineData("""{"type":"quiz","title":"Manche 1","questions":[],"answerSecond":30}""")]
    [InlineData("""{"type":"quiz","title":"Manche 1","questions":[{"text":"Question ?","choices":[],"answer":"Oui"}]}""")]
    [InlineData("""{"type":"quiz","title":"Manche 1","questions":[{"text":"Question ?","choices":[{"text":"Oui","corect":true}]}]}""")]
    public void Deserialize_UnknownProperty_Throws(string round) =>
        Assert.Throws<JsonException>(() => ReadRound(round));

    [Fact]
    public void Validate_ValidRoundQuestionAndChoice_ReportNothing()
    {
        var quiz = (QuizRoundDescriptor)Read(CompletePack).Rounds[0];

        Assert.Empty(Validate(quiz));
        Assert.Empty(Validate(quiz.Questions[0]));
        Assert.Empty(Validate(quiz.Questions[0].Choices[0]));
    }

    [Theory]
    [InlineData(4, nameof(QuizRoundDescriptor.AnswerSeconds))]
    [InlineData(121, nameof(QuizRoundDescriptor.AnswerSeconds))]
    [InlineData(-1, nameof(QuizRoundDescriptor.Points))]
    [InlineData(10_001, nameof(QuizRoundDescriptor.Points))]
    [InlineData(-1, nameof(QuizRoundDescriptor.SpeedBonus))]
    [InlineData(10_001, nameof(QuizRoundDescriptor.SpeedBonus))]
    public void Validate_RoundValueOutOfBounds_ReportsIt(int value, string property)
    {
        var round = Round();
        round = property switch
        {
            nameof(QuizRoundDescriptor.AnswerSeconds) => round with { AnswerSeconds = value },
            nameof(QuizRoundDescriptor.Points) => round with { Points = value },
            _ => round with { SpeedBonus = value },
        };

        Assert.Equal([property], Validate(round));
    }

    [Theory]
    [InlineData(5)]
    [InlineData(120)]
    public void Validate_AnswerSecondsAtBounds_ReportsNothing(int seconds)
    {
        Assert.Empty(Validate(Round() with { AnswerSeconds = seconds }));
        Assert.Empty(Validate(Question() with { AnswerSeconds = seconds }));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(51)]
    public void Validate_QuestionCountOutOfBounds_ReportsQuestions(int count) =>
        Assert.Equal([nameof(QuizRoundDescriptor.Questions)], Validate(Round() with { Questions = [.. Enumerable.Repeat(Question(), count)] }));

    [Theory]
    [InlineData(0)]
    [InlineData(201)]
    public void Validate_QuestionTextLengthOutOfBounds_ReportsText(int length) =>
        Assert.Equal([nameof(QuizQuestion.Text)], Validate(Question() with { Text = new string('a', length) }));

    [Theory]
    [InlineData(4)]
    [InlineData(121)]
    public void Validate_QuestionAnswerSecondsOutOfBounds_ReportsAnswerSeconds(int seconds) =>
        Assert.Equal([nameof(QuizQuestion.AnswerSeconds)], Validate(Question() with { AnswerSeconds = seconds }));

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    public void Validate_ChoiceCountOutOfBounds_ReportsChoices(int count) =>
        Assert.Equal([nameof(QuizQuestion.Choices)], Validate(Question() with { Choices = [.. Enumerable.Range(1, count).Select(i => new QuizChoice { Text = $"{i}" })] }));

    [Theory]
    [InlineData(0)]
    [InlineData(81)]
    public void Validate_ChoiceTextLengthOutOfBounds_ReportsText(int length) =>
        Assert.Equal([nameof(QuizChoice.Text)], Validate(new QuizChoice { Text = new string('a', length) }));

    private static PackDescriptor Read(string json) =>
        JsonSerializer.Deserialize<PackDescriptor>(json, PackJsonOptions.Default)
        ?? throw new InvalidOperationException("The descriptor is null.");

    private static QuizRoundDescriptor ReadRound(string round) =>
        Assert.IsType<QuizRoundDescriptor>(Assert.Single(Read($$"""{"formatVersion":1,"title":"Soirée quiz","rounds":[{{round}}]}""").Rounds));

    private static QuizRoundDescriptor Round() => new() { Title = "Manche 1", Questions = [Question()] };

    private static QuizQuestion Question() => new()
    {
        Text = "Capitale de la France ?",
        Choices = [new QuizChoice { Text = "Paris", Correct = true }, new QuizChoice { Text = "Lyon" }],
    };

    // The same attributes produce the schema constraints. The validator checks one object, not the objects it contains.
    private static List<string> Validate(object instance)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(instance, new ValidationContext(instance), results, validateAllProperties: true);
        return [.. results.SelectMany(r => r.MemberNames)];
    }
}
