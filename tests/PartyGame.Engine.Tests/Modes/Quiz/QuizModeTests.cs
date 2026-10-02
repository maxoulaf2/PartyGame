using PartyGame.Contracts;
using PartyGame.Contracts.Packs;
using PartyGame.Engine.Modes;
using PartyGame.Engine.Modes.Quiz;

namespace PartyGame.Engine.Tests.Modes.Quiz;

public sealed class QuizModeTests
{
    private const string RoundPath = "$.rounds[1]";

    private static readonly QuizMode _mode = new();

    [Fact]
    public void Validate_ConsistentRound_ReportsNothing()
    {
        // Given
        var round = Round(
            Question(("Paris", true), ("Lyon", false), ("Marseille", false)),
            Question(("Pari", false), ("Paris", true)));

        // When
        var problems = _mode.Validate(round, RoundPath);

        // Then
        Assert.Empty(problems);
    }

    [Fact]
    public void Validate_QuestionWithoutCorrectChoice_ReportsTheQuestion()
    {
        // Given
        var round = Round(Question(("Paris", true), ("Lyon", false)), Question(("Oui", false), ("Non", false)));

        // When
        var problems = _mode.Validate(round, RoundPath);

        // Then
        var problem = Assert.Single(problems);
        Assert.Equal(PackProblemCode.QuizCorrectChoiceMissing, problem.Code);
        Assert.Equal("pack.json", problem.File);
        Assert.Equal("$.rounds[1].questions[1]", problem.Path);
        Assert.Empty(problem.Parameters);
    }

    [Fact]
    public void Validate_QuestionWithSeveralCorrectChoices_ReportsTheQuestion()
    {
        // Given
        var round = Round(Question(("Paris", true), ("Lyon", true), ("Marseille", true)));

        // When
        var problems = _mode.Validate(round, RoundPath);

        // Then
        var problem = Assert.Single(problems);
        Assert.Equal(PackProblemCode.QuizCorrectChoiceDuplicated, problem.Code);
        Assert.Equal("$.rounds[1].questions[0]", problem.Path);
    }

    [Theory]
    [InlineData("Le Mans", "Le Mans")]
    [InlineData("Le Mans", "le mans")]
    [InlineData("Évry", "evry")]
    [InlineData("Le Mans", "  Le   Mans ")]
    [InlineData("Saint-Étienne", "SAINT-ETIENNE")]
    public void Validate_ChoicesSameIgnoringCaseAccentsAndSpaces_ReportsTheSecondOne(string first, string second)
    {
        // Given
        var round = Round(Question(("Paris", true), (first, false), ("Lyon", false), (second, false)));

        // When
        var problems = _mode.Validate(round, RoundPath);

        // Then
        var problem = Assert.Single(problems);
        Assert.Equal(PackProblemCode.QuizChoiceDuplicated, problem.Code);
        Assert.Equal("pack.json", problem.File);
        Assert.Equal("$.rounds[1].questions[0].choices[3]", problem.Path);
        Assert.Equal(second, Assert.Single(problem.Parameters, p => p.Key == "choice").Value);
    }

    [Theory]
    [InlineData("Le Mans", "Le Man")]
    [InlineData("Saint-Étienne", "Saint Étienne")]
    [InlineData("1", "1.0")]
    public void Validate_ChoicesThatDiffer_ReportsNothing(string first, string second) =>
        Assert.Empty(_mode.Validate(Round(Question((first, true), (second, false))), RoundPath));

    [Fact]
    public void Validate_SeveralProblems_ReportsThemAllInOrder()
    {
        // Given
        var round = Round(
            Question(("Oui", false), ("oui", false), ("OUI", false)),
            Question(("Paris", true), ("Lyon", false)),
            Question(("Paris", true), ("Lyon", true)));

        // When
        var problems = _mode.Validate(round, RoundPath);

        // Then
        Assert.Equal(
            [
                (PackProblemCode.QuizCorrectChoiceMissing, "$.rounds[1].questions[0]"),
                (PackProblemCode.QuizChoiceDuplicated, "$.rounds[1].questions[0].choices[1]"),
                (PackProblemCode.QuizChoiceDuplicated, "$.rounds[1].questions[0].choices[2]"),
                (PackProblemCode.QuizCorrectChoiceDuplicated, "$.rounds[1].questions[2]"),
            ],
            problems.Select(p => (p.Code, p.Path)));
    }

    [Fact]
    public void Validate_ThroughTheInterface_ChecksTheDescriptor()
    {
        // Given
        IGameMode mode = _mode;

        // When
        var problems = mode.Validate(Round(Question(("Oui", false), ("Non", false))), "$.rounds[0]");

        // Then
        Assert.Equal("$.rounds[0].questions[0]", Assert.Single(problems).Path);
    }

    [Fact]
    public void Start_BeforeTheQuestionsArePlayed_FinishesTheRoundAtOnce()
    {
        // Given: the questions are played from US-E08-02
        var round = Round(Question(("Paris", true), ("Lyon", false)));

        // When
        var transition = _mode.Start(round, Games.LobbyWith("Zoé"), Games.Context());

        // Then
        Assert.True(transition.IsFinished);
        Assert.Null(transition.Rejection);
        Assert.Empty(transition.Effects);
    }

    private static QuizRoundDescriptor Round(params QuizQuestion[] questions) =>
        new() { Title = "Culture générale", Questions = [.. questions] };

    private static QuizQuestion Question(params (string Text, bool Correct)[] choices) => new()
    {
        Text = "Question ?",
        Choices = [.. choices.Select(choice => new QuizChoice { Text = choice.Text, Correct = choice.Correct })],
    };
}
