using PartyGame.Contracts;
using PartyGame.Contracts.Buzzer;
using PartyGame.Contracts.Packs;
using PartyGame.Engine.Effects;
using PartyGame.Engine.Inputs;
using PartyGame.Engine.Modes.Buzzer;
using EngineBuzzer = PartyGame.Engine.Buzzers.Buzzer;

namespace PartyGame.Engine.Tests.Modes.Buzzer;

/// <summary>
/// A question of a round of buzzer questions: announced, asked by the game master, which opens the buzzer, then given to
/// the player who pressed first.
/// </summary>
public sealed class BuzzerModeTests
{
    private static readonly string[] _players = ["Zoé", "Max", "Léa"];

    private static readonly BuzzerRoundDescriptor _round =
        BuzzerGames.Round(BuzzerGames.PaintingQuestion, BuzzerGames.IllustratedQuestion, BuzzerGames.LastQuestion);

    private static GameState NewGame => BuzzerGames.Started(_round, _players);

    [Fact]
    public void Validate_RoundWithinTheConstraintsOfItsDescriptor_ReportsNothing() =>
        Assert.Empty(BuzzerGames.Mode.Validate(_round, "$.rounds[0]"));

    [Fact]
    public void Start_Round_AnnouncesTheFirstQuestionBuzzerClosed()
    {
        // When
        var transition = BuzzerGames.Mode.Start(_round, Games.NewLobby(), Games.Context());

        // Then
        Assert.False(transition.IsFinished);
        Assert.Empty(transition.Effects);
        var round = Assert.IsType<BuzzerRound>(transition.State);
        Assert.Equal((1, BuzzerPhase.Ready), (round.QuestionNumber, round.Phase));
        Assert.False(round.Buzzer.IsOpen);
    }

    [Fact]
    public void Handle_AskQuestion_OpensTheBuzzer()
    {
        // Given
        var state = NewGame;

        // When
        var transition = BuzzerGames.Engine.Handle(state, BuzzerGames.AskQuestion(state), Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        var round = BuzzerGames.RoundOf(transition.State);
        Assert.Equal(BuzzerPhase.Open, round.Phase);
        Assert.Equal((1, Games.Now), (round.Buzzer.Opening, round.Buzzer.OpenedAt));
    }

    [Fact]
    public void Handle_AskQuestionTwice_IsRejectedAsObsolete()
    {
        // Given
        var state = BuzzerGames.Asked(NewGame);

        // When
        var transition = BuzzerGames.Engine.Handle(state, BuzzerGames.AskQuestion(state), Games.Context());

        // Then
        Assert.Equal(RejectionReason.PhaseMismatch, transition.Rejection);
        Assert.Same(state, transition.State);
    }

    [Fact]
    public void Handle_AskAnotherQuestion_IsRejected()
    {
        // Given
        var state = NewGame;

        // When
        var transition = BuzzerGames.Engine.Handle(state, BuzzerGames.AskQuestion(state, questionNumber: 2), Games.Context());

        // Then
        Assert.Equal(RejectionReason.QuestionMismatch, transition.Rejection);
        Assert.Same(state, transition.State);
    }

    [Fact]
    public void Handle_FirstBuzz_StartsTheArbitrationWindow()
    {
        // Given
        var state = BuzzerGames.Asked(NewGame);

        // When
        var transition = BuzzerGames.Engine.Handle(state, BuzzerGames.Buzz(state, 2), Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        Assert.Equal(BuzzerPhase.Arbitrating, BuzzerGames.RoundOf(transition.State).Phase);
        Assert.Equal(
            new ScheduleTimer(EngineBuzzer.ArbitrationTimer, Games.Now.AddMilliseconds(250)) { RoundId = state.CurrentRound!.Id },
            Assert.Single(transition.Effects));
    }

    [Fact]
    public void Handle_ArbitrationElapsed_GivesTheHandToTheEarliestPress()
    {
        // Given: Max's buzz arrived first, but Léa pressed earlier
        var state = BuzzerGames.Asked(NewGame);
        state = BuzzerGames.Accepted(state, BuzzerGames.Buzz(state, 2, Games.Now.AddMilliseconds(120), Games.Now.AddMilliseconds(130)));
        state = BuzzerGames.Accepted(state, BuzzerGames.Buzz(state, 3, Games.Now.AddMilliseconds(80), Games.Now.AddMilliseconds(200)));

        // When
        var transition = BuzzerGames.Engine.Handle(state, BuzzerGames.ArbitrationElapsed(state), Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        var round = BuzzerGames.RoundOf(transition.State);
        Assert.Equal((BuzzerPhase.Answering, Games.PlayerIdOf(3)), (round.Phase, round.Buzzer.Winner));
    }

    [Fact]
    public void Handle_BuzzBeforeTheQuestionIsAsked_IsRejected()
    {
        // Given
        var state = NewGame;

        // When
        var transition = BuzzerGames.Engine.Handle(state, BuzzerGames.Buzz(state, 1), Games.Context());

        // Then
        Assert.Equal(RejectionReason.BuzzerClosed, transition.Rejection);
        Assert.Same(state, transition.State);
    }

    [Fact]
    public void Handle_BuzzOnceTheWinnerHasTheHand_IsRejected()
    {
        // Given
        var state = BuzzerGames.Answering(NewGame, (1, 50));

        // When
        var transition = BuzzerGames.Engine.Handle(state, BuzzerGames.Buzz(state, 2), Games.Context());

        // Then
        Assert.Equal(RejectionReason.BuzzerClosed, transition.Rejection);
    }

    [Fact]
    public void Handle_BuzzOnAnotherQuestion_IsRejected()
    {
        // Given
        var state = BuzzerGames.Asked(NewGame);

        // When
        var transition = BuzzerGames.Engine.Handle(state, BuzzerGames.Buzz(state, 1, questionNumber: 2), Games.Context());

        // Then
        Assert.Equal(RejectionReason.QuestionMismatch, transition.Rejection);
    }

    [Fact]
    public void Handle_BuzzOnAnotherOpening_IsRejected()
    {
        // Given
        var state = BuzzerGames.Asked(NewGame);

        // When
        var transition = BuzzerGames.Engine.Handle(state, BuzzerGames.Buzz(state, 1, opening: 0), Games.Context());

        // Then
        Assert.Equal(RejectionReason.BuzzerOpeningMismatch, transition.Rejection);
    }

    [Fact]
    public void Handle_SecondBuzzOfAPlayer_IsRejected()
    {
        // Given
        var state = BuzzerGames.Buzzed(NewGame, (1, 50));

        // When
        var transition = BuzzerGames.Engine.Handle(state, BuzzerGames.Buzz(state, 1), Games.Context());

        // Then
        Assert.Equal(RejectionReason.AlreadyBuzzed, transition.Rejection);
    }

    [Fact]
    public void Handle_BuzzOfABlockedPlayer_IsRejected()
    {
        // Given
        var state = BuzzerGames.WithBlocked(BuzzerGames.Asked(NewGame), 1);

        // When
        var transition = BuzzerGames.Engine.Handle(state, BuzzerGames.Buzz(state, 1), Games.Context());

        // Then
        Assert.Equal(RejectionReason.PlayerBlocked, transition.Rejection);
    }

    [Fact]
    public void Handle_PlayerJoinedDuringTheQuestion_MayBuzz()
    {
        // Given: Léa joined once the buzzer was open
        var state = BuzzerGames.Accepted(BuzzerGames.Asked(BuzzerGames.Started(_round, "Zoé", "Max")), Games.Join("Léa", player: 3));

        // When
        var transition = BuzzerGames.Engine.Handle(state, BuzzerGames.Buzz(state, 3), Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        Assert.Equal(0, transition.State.Players.Single(player => player.Nickname == "Léa").Score);
    }

    [Fact]
    public void Handle_ObsoleteArbitrationTimer_IsRejected()
    {
        // Given
        var state = BuzzerGames.Answering(NewGame, (1, 50));
        var timer = new TimerElapsed(EngineBuzzer.ArbitrationTimer, Games.Now.AddMilliseconds(250)) { RoundId = state.CurrentRound!.Id };

        // When
        var transition = BuzzerGames.Engine.Handle(state, timer, Games.Context());

        // Then
        Assert.Equal(RejectionReason.UnexpectedTimer, transition.Rejection);
    }

    [Fact]
    public void Handle_GameMasterIntentOfAnotherMode_IsUnsupported()
    {
        // Given
        var state = NewGame;
        var input = new GameMasterRoundInput(new Contracts.Quiz.QuizShowQuestion(state.CurrentRound!.Id, 1), Games.Now);

        // When
        var transition = BuzzerGames.Engine.Handle(state, input, Games.Context());

        // Then
        Assert.Equal(RejectionReason.IntentUnsupported, transition.Rejection);
    }

    [Fact]
    public void Handle_GameResumedDuringTheArbitration_GoesOnWithTheTimeLeft()
    {
        // Given: saved 100 ms into the window, resumed half an hour later
        var state = BuzzerGames.Buzzed(NewGame, (1, 0));
        var savedAt = Games.Now.AddMilliseconds(100);
        var resumedAt = savedAt.AddMinutes(30);

        // When
        var transition = BuzzerGames.Engine.Handle(state, new GameResumed(savedAt), new GameContext(resumedAt, new Random(42)));

        // Then
        Assert.Null(transition.Rejection);
        var arbitrateAt = resumedAt.AddMilliseconds(150);
        Assert.Equal(arbitrateAt, BuzzerGames.RoundOf(transition.State).Buzzer.ArbitrateAt);
        Assert.Equal(
            new ScheduleTimer(EngineBuzzer.ArbitrationTimer, arbitrateAt) { RoundId = state.CurrentRound!.Id },
            Assert.Single(transition.Effects));
    }

    [Theory]
    [InlineData(BuzzerPhase.Ready)]
    [InlineData(BuzzerPhase.Open)]
    [InlineData(BuzzerPhase.Answering)]
    public void Handle_GameResumedOutsideTheArbitration_SchedulesNothing(BuzzerPhase phase)
    {
        // Given
        var state = phase switch
        {
            BuzzerPhase.Ready => NewGame,
            BuzzerPhase.Open => BuzzerGames.Asked(NewGame),
            _ => BuzzerGames.Answering(NewGame, (1, 0)),
        };

        // When
        var transition = BuzzerGames.Engine.Handle(state, new GameResumed(Games.Now), new GameContext(Games.Now.AddMinutes(5), new Random(42)));

        // Then
        Assert.Null(transition.Rejection);
        Assert.Equal(phase, BuzzerGames.RoundOf(transition.State).Phase);
        Assert.DoesNotContain(transition.Effects, effect => effect is ScheduleTimer);
    }

    [Fact]
    public void ProjectForPlayer_EachPhase_ShowsTheBuzzerOfThePlayer()
    {
        // Given: Zoé pressed first, Max pressed too, Léa is blocked
        var ready = NewGame;
        var buzzed = BuzzerGames.WithBlocked(BuzzerGames.Buzzed(NewGame, (1, 10), (2, 20)), 3);
        var answering = BuzzerGames.Accepted(buzzed, BuzzerGames.ArbitrationElapsed(buzzed));

        // Then
        Assert.Equal([BuzzerButtonState.Closed, BuzzerButtonState.Closed, BuzzerButtonState.Closed], ButtonsOf(ready));
        Assert.Equal([BuzzerButtonState.Buzzed, BuzzerButtonState.Buzzed, BuzzerButtonState.Blocked], ButtonsOf(buzzed));
        Assert.Equal([BuzzerButtonState.Won, BuzzerButtonState.Lost, BuzzerButtonState.Blocked], ButtonsOf(answering));
        Assert.All(answering.Players, player => Assert.Equal("Zoé", PlayerView(answering, player).Winner));
    }

    [Fact]
    public void ProjectForDisplay_QuestionAsked_ShowsItsTextAndImage()
    {
        // Given
        var state = BuzzerGames.Asked(BuzzerGames.AtQuestion(NewGame, 1));

        // When
        var view = Assert.IsType<BuzzerDisplayView>(BuzzerGames.Snapshots.ForDisplay(state).RoundView);

        // Then
        Assert.Equal(BuzzerGames.IllustratedQuestion.Text, view.Text);
        Assert.Equal(state.Media.UrlOf(BuzzerGames.Flag), view.ImageUrl);
        Assert.Equal((2, 3, BuzzerQuestionPhase.Open), (view.QuestionNumber, view.QuestionCount, view.Phase));
    }

    [Fact]
    public void ProjectForGameMaster_QuestionAnnounced_ShowsTheQuestionAndItsAnswer()
    {
        // When
        var view = Assert.IsType<BuzzerGameMasterView>(BuzzerGames.Snapshots.ForGameMaster(NewGame).RoundView);

        // Then
        Assert.Equal(
            (BuzzerQuestionPhase.Ready, BuzzerGames.PaintingQuestion.Text, BuzzerGames.PaintingQuestion.Answer, (string?)null),
            (view.Phase, view.Text, view.Answer, view.Winner));
    }

    [Fact]
    public void StepOf_Round_IsTheQuestionInProgress() =>
        Assert.Equal(new RoundStep(2, 3), BuzzerGames.Mode.StepOf(BuzzerGames.RoundOf(BuzzerGames.AtQuestion(NewGame, 1))));

    [Fact]
    public void LocateMedia_ImageOfAQuestion_IsItsNumber() =>
        Assert.Equal(2, BuzzerGames.Mode.LocateMedia(BuzzerGames.RoundOf(NewGame), BuzzerGames.Flag));

    [Fact]
    public void LocateMedia_UnknownFile_IsNull() =>
        Assert.Null(BuzzerGames.Mode.LocateMedia(BuzzerGames.RoundOf(NewGame), new MediaPath("images/autre.png")));

    private static BuzzerButtonState[] ButtonsOf(GameState state) =>
        [.. state.Players.Select(player => PlayerView(state, player).Buzzer)];

    private static BuzzerPlayerView PlayerView(GameState state, Player player) =>
        Assert.IsType<BuzzerPlayerView>(BuzzerGames.Snapshots.ForPlayer(state, player).RoundView);
}
