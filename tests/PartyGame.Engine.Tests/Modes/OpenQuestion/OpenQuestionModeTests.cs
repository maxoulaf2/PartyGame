using PartyGame.Contracts.Packs;
using PartyGame.Engine.Modes.OpenQuestion;

namespace PartyGame.Engine.Tests.Modes.OpenQuestion;

public sealed class OpenQuestionModeTests
{
    private static readonly OpenQuestionMode _mode = new();

    private static readonly OpenQuestionDescriptor _question = new()
    {
        Text = "Qui a peint La Joconde ?",
        Answer = "Léonard de Vinci",
        AcceptedAnswers = ["Vinci", "Leonardo da Vinci"],
    };

    private static readonly OpenQuestionRoundDescriptor _round = new() { Title = "Réponses libres", MaxLength = 20, Questions = [_question] };

    [Fact]
    public void Validate_ValidRound_ReportsNothing()
    {
        Assert.Empty(_mode.Validate(_round, "$.rounds[0]"));
        Assert.Empty(_mode.Validate(WithQuestion(new() { Text = "Année ?", Answer = "1789", AcceptedAnswers = ["01789"], InputMode = OpenQuestionInputMode.Numeric }), "$.rounds[0]"));
    }

    [Fact]
    public void Validate_AnswerLongerThanMaxLength_ReportsTheAnswer() =>
        Assert.Equal(
            ["OpenQuestionAnswerLengthOutOfRange $.rounds[1].questions[0].answer max=20"],
            Describe(WithQuestion(_question with { Answer = new string('a', 21) })));

    [Fact]
    public void Validate_AnswerBeyondTheBoundsOfItsProperty_LeavesItToTheLoading()
    {
        Assert.Empty(Describe(WithQuestion(_question with { Answer = "" })));
        Assert.Empty(Describe(WithQuestion(_question with { Answer = new string('a', 101) })));
    }

    [Theory]
    [InlineData("")]
    [InlineData("Léonard de Vinci, peintre")]
    public void Validate_VariantEmptyOrLongerThanMaxLength_ReportsTheVariant(string variant) =>
        Assert.Equal(
            ["OpenQuestionAnswerLengthOutOfRange $.rounds[1].questions[0].acceptedAnswers[1] max=20"],
            Describe(WithQuestion(_question with { AcceptedAnswers = ["Vinci", variant] })));

    [Theory]
    [InlineData("!")]
    [InlineData("Les")]
    public void Validate_AnswerEmptyOnceNormalized_ReportsIt(string answer) =>
        Assert.Equal(
            [$"OpenQuestionAnswerEmpty $.rounds[1].questions[0].answer answer={answer}", $"OpenQuestionAnswerEmpty $.rounds[1].questions[0].acceptedAnswers[0] answer={answer}"],
            Describe(WithQuestion(_question with { Answer = answer, AcceptedAnswers = [answer] })));

    [Theory]
    [InlineData("leonard de vinci")]
    [InlineData("Le Léonard de Vinci!")]
    public void Validate_VariantSameAsTheAnswerOnceNormalized_ReportsTheVariant(string variant) =>
        Assert.Equal(
            [$"OpenQuestionAnswerDuplicated $.rounds[1].questions[0].acceptedAnswers[0] answer={variant}"],
            Describe(WithQuestion(_question with { AcceptedAnswers = [variant] })));

    [Fact]
    public void Validate_VariantSameAsAnEarlierVariantOnceNormalized_ReportsTheLaterOne() =>
        Assert.Equal(
            ["OpenQuestionAnswerDuplicated $.rounds[1].questions[0].acceptedAnswers[2] answer=VINCI"],
            Describe(WithQuestion(_question with { AcceptedAnswers = ["Vinci", "Da Vinci", "VINCI"] })));

    [Fact]
    public void Validate_NumericQuestionWithOtherCharactersThanDigits_ReportsEachOne() =>
        Assert.Equal(
            ["OpenQuestionAnswerNotNumeric $.rounds[1].questions[0].answer answer=1 789", "OpenQuestionAnswerNotNumeric $.rounds[1].questions[0].acceptedAnswers[0] answer=-1789"],
            Describe(WithQuestion(new() { Text = "Année ?", Answer = "1 789", AcceptedAnswers = ["-1789", "1789"], InputMode = OpenQuestionInputMode.Numeric })));

    [Fact]
    public void Validate_ProblemsInSeveralQuestions_ReportsThemAll()
    {
        var round = _round with { Questions = [_question with { AcceptedAnswers = ["!"] }, _question, _question with { AcceptedAnswers = ["Vinci", "vinci"] }] };

        Assert.Equal(
            ["OpenQuestionAnswerEmpty $.rounds[1].questions[0].acceptedAnswers[0] answer=!", "OpenQuestionAnswerDuplicated $.rounds[1].questions[2].acceptedAnswers[1] answer=vinci"],
            Describe(round));
    }

    private static OpenQuestionRoundDescriptor WithQuestion(OpenQuestionDescriptor question) => _round with { Questions = [question] };

    private static IEnumerable<string> Describe(OpenQuestionRoundDescriptor round) =>
        _mode.Validate(round, "$.rounds[1]").Select(problem =>
            $"{problem.Code} {problem.Path}{string.Concat(problem.Parameters.Select(parameter => $" {parameter.Key}={parameter.Value}"))}");
}
