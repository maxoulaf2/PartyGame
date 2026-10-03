using PartyGame.Contracts;
using PartyGame.Contracts.Packs;
using PartyGame.Contracts.Quiz;
using PartyGame.Engine.Inputs;
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
    public void Start_Round_PresentsItsFirstQuestionWithoutEffect()
    {
        // Given
        var round = Round(Question(("Paris", true), ("Lyon", false)), Question(("Oui", true), ("Non", false)));

        // When
        var transition = _mode.Start(round, Games.LobbyWith("Zoé"), Games.Context());

        // Then
        Assert.False(transition.IsFinished);
        Assert.Null(transition.Rejection);
        Assert.Empty(transition.Effects);
        var state = Assert.IsType<QuizRound>(transition.State);
        Assert.Equal((0, QuizPhase.Presentation), (state.QuestionIndex, state.Phase));
        Assert.Same(round, state.Descriptor);
    }

    [Fact]
    public void Start_WithoutShuffle_KeepsTheOrderOfTheDescriptor()
    {
        // Given
        var round = Round(QuizGames.CapitalQuestion);

        // When
        var state = (QuizRound)_mode.Start(round, Games.LobbyWith("Zoé"), Games.Context()).State;

        // Then
        Assert.Equal([0, 1, 2, 3], state.ChoiceOrder);
    }

    [Fact]
    public void Start_WithShuffle_ShufflesTheChoicesReproduciblyWithTheSeed()
    {
        // Given
        var round = Round(QuizGames.CapitalQuestion) with { ShuffleChoices = true };
        var lobby = Games.LobbyWith("Zoé");

        // When
        var orders = Enumerable.Range(0, 20)
            .Select(seed => ((QuizRound)_mode.Start(round, lobby, Games.Context(seed)).State).ChoiceOrder)
            .ToList();
        var again = ((QuizRound)_mode.Start(round, lobby, Games.Context(seed: 7)).State).ChoiceOrder;

        // Then: every order holds each choice once, the seed alone decides it, and the seeds do not all agree
        Assert.All(orders, order => Assert.Equal([0, 1, 2, 3], order.Order()));
        Assert.Equal(orders[7], again);
        Assert.True(orders.Select(order => string.Join(",", order)).Distinct().Count() > 1);
    }

    [Fact]
    public void Handle_Timer_IsRejected()
    {
        // Given: the round scheduled none
        var state = QuizGames.Started([Round(QuizGames.CapitalQuestion)], ["Zoé"]);
        var timer = new TimerElapsed(new TimerId("countdown"), Games.Now) { RoundId = state.CurrentRound!.Id };

        // When
        var transition = QuizGames.Engine.Handle(state, timer, Games.Context());

        // Then
        Assert.Equal(RejectionReason.UnexpectedTimer, transition.Rejection);
        Assert.Same(state, transition.State);
    }

    [Fact]
    public void Handle_IntentOfAPlayer_IsRejectedUntilTheAnswersOpen()
    {
        // Given: the intents of the contracts are placeholders until US-E08-03
        var state = QuizGames.Started([Round(QuizGames.CapitalQuestion)], ["Zoé"]);
        var intent = new PlayerRoundInput(Games.PlayerIdOf(1), new QuizPlayerIntent(state.CurrentRound!.Id), Games.Now);

        // When
        var transition = QuizGames.Engine.Handle(state, intent, Games.Context());

        // Then
        Assert.Equal(RejectionReason.IntentUnsupported, transition.Rejection);
        Assert.Same(state, transition.State);
    }

    [Fact]
    public void Handle_IntentOfTheGameMaster_IsRejectedUntilTheAnswersOpen()
    {
        // Given
        var state = QuizGames.Started([Round(QuizGames.CapitalQuestion)], ["Zoé"]);
        var intent = new GameMasterRoundInput(new QuizGameMasterIntent(state.CurrentRound!.Id), Games.Now);

        // When
        var transition = QuizGames.Engine.Handle(state, intent, Games.Context());

        // Then
        Assert.Equal(RejectionReason.IntentUnsupported, transition.Rejection);
        Assert.Same(state, transition.State);
    }

    [Fact]
    public void Snapshots_GameStarted_ShowTheFirstQuestionInPresentationToEveryRole()
    {
        // Given
        var state = QuizGames.Started([Round(QuizGames.CapitalQuestion, QuizGames.LastQuestion)], ["Zoé"]);

        // When
        var display = Assert.IsType<QuizDisplayView>(QuizGames.Snapshots.ForDisplay(state).RoundView);
        var gameMaster = Assert.IsType<QuizGameMasterView>(QuizGames.Snapshots.ForGameMaster(state).RoundView);
        var player = Assert.IsType<QuizPlayerView>(QuizGames.Snapshots.ForPlayer(state, state.Players[0]).RoundView);

        // Then
        var question = (1, 2, QuizQuestionPhase.Presentation, QuizGames.CapitalQuestion.Text);
        Assert.Equal(question, (display.QuestionNumber, display.QuestionCount, display.Phase, display.Text));
        Assert.Equal(question, (gameMaster.QuestionNumber, gameMaster.QuestionCount, gameMaster.Phase, gameMaster.Text));
        Assert.Equal(question, (player.QuestionNumber, player.QuestionCount, player.Phase, player.Text));
    }

    [Fact]
    public void ProjectForDisplay_Presentation_ShowsTheChoicesWithTheirLettersInTheOrderOfTheDescriptor()
    {
        // Given
        var state = QuizGames.Started([Round(QuizGames.CapitalQuestion)], ["Zoé"]);

        // When
        var view = (QuizDisplayView)QuizGames.Mode.ProjectForDisplay(QuizGames.RoundOf(state), state);

        // Then
        Assert.Equal(
            [
                new QuizChoiceView(QuizChoiceLetter.A, "Canberra"),
                new QuizChoiceView(QuizChoiceLetter.B, "Sydney"),
                new QuizChoiceView(QuizChoiceLetter.C, "Melbourne"),
                new QuizChoiceView(QuizChoiceLetter.D, "Perth"),
            ],
            view.Choices);
        Assert.Null(view.ImageUrl);
    }

    [Fact]
    public void ProjectForDisplay_IllustratedQuestion_ShowsTheUrlOfItsImage()
    {
        // Given
        var state = QuizGames.Started([Round(QuizGames.IllustratedQuestion)], ["Zoé"]);

        // When
        var view = (QuizDisplayView)QuizGames.Mode.ProjectForDisplay(QuizGames.RoundOf(state), state);

        // Then
        Assert.Equal(state.Media.UrlOf(QuizGames.Flag), view.ImageUrl);
    }

    [Fact]
    public void ProjectForGameMaster_ShuffledChoices_MarksTheCorrectChoiceAtItsShownPosition()
    {
        // Given: the correct answer, first in the descriptor, moves with the shuffle
        var round = Round(QuizGames.CapitalQuestion) with { ShuffleChoices = true };
        var state = QuizGames.Started([round], ["Zoé"]);
        var order = QuizGames.RoundOf(state).ChoiceOrder;

        // When
        var view = (QuizGameMasterView)QuizGames.Mode.ProjectForGameMaster(QuizGames.RoundOf(state), state);

        // Then
        var correct = Assert.Single(view.Choices, choice => choice.Correct);
        Assert.Equal("Canberra", correct.Text);
        Assert.Equal((QuizChoiceLetter)order.IndexOf(0), correct.Letter);
        Assert.Equal([QuizChoiceLetter.A, QuizChoiceLetter.B, QuizChoiceLetter.C, QuizChoiceLetter.D], view.Choices.Select(c => c.Letter));
    }

    [Fact]
    public void Projections_ShuffledChoices_ShowTheSameOrderOnEveryScreen()
    {
        // Given
        var round = Round(QuizGames.CapitalQuestion) with { ShuffleChoices = true };
        var state = QuizGames.Started([round], ["Zoé", "Max"]);
        var quiz = QuizGames.RoundOf(state);

        // When
        var display = (QuizDisplayView)QuizGames.Mode.ProjectForDisplay(quiz, state);
        var gameMaster = (QuizGameMasterView)QuizGames.Mode.ProjectForGameMaster(quiz, state);
        var phones = state.Players.Select(p => (QuizPlayerView)QuizGames.Mode.ProjectForPlayer(quiz, state, p)).ToList();

        // Then
        var expected = quiz.ChoiceOrder.Select(i => QuizGames.CapitalQuestion.Choices[i].Text).ToList();
        Assert.Equal(expected, display.Choices.Select(c => c.Text));
        Assert.Equal(expected, gameMaster.Choices.Select(c => c.Text));
        Assert.All(phones, phone => Assert.Equal(display.Choices, phone.Choices));
    }

    [Fact]
    public void ProjectForPlayer_PlayerJoinedDuringThePresentation_SeesTheQuestionLikeTheOthers()
    {
        // Given
        var state = QuizGames.Started([Round(QuizGames.CapitalQuestion)], ["Zoé"]);
        state = QuizGames.Accepted(state, Games.Join("Max", player: 2));

        // When
        var zoe = (QuizPlayerView)QuizGames.Snapshots.ForPlayer(state, state.Players[0]).RoundView!;
        var max = (QuizPlayerView)QuizGames.Snapshots.ForPlayer(state, state.Players[1]).RoundView!;

        // Then
        Assert.Equal((zoe.QuestionNumber, zoe.Phase, zoe.Text), (max.QuestionNumber, max.Phase, max.Text));
        Assert.Equal(zoe.Choices, max.Choices);
    }

    [Fact]
    public void ProjectForPlayer_LaterQuestion_ShowsItsNumberOnTheCount()
    {
        // Given
        var started = QuizGames.Started([Round(QuizGames.CapitalQuestion, QuizGames.IllustratedQuestion, QuizGames.LastQuestion)], ["Zoé"]);
        var state = QuizGames.AtQuestion(started, 2);

        // When
        var view = (QuizPlayerView)QuizGames.Mode.ProjectForPlayer(QuizGames.RoundOf(state), state, state.Players[0]);

        // Then
        Assert.Equal((3, 3, QuizGames.LastQuestion.Text), (view.QuestionNumber, view.QuestionCount, view.Text));
    }

    private static QuizRoundDescriptor Round(params QuizQuestion[] questions) =>
        new() { Title = "Culture générale", Questions = [.. questions] };

    private static QuizQuestion Question(params (string Text, bool Correct)[] choices) => new()
    {
        Text = "Question ?",
        Choices = [.. choices.Select(choice => new QuizChoice { Text = choice.Text, Correct = choice.Correct })],
    };
}
