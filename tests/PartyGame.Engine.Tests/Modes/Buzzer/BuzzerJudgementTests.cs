using PartyGame.Contracts.Buzzer;
using PartyGame.Contracts.Packs;
using PartyGame.Engine.Effects;
using PartyGame.Engine.Modes.Buzzer;
using EngineBuzzer = PartyGame.Engine.Buzzers.Buzzer;

namespace PartyGame.Engine.Tests.Modes.Buzzer;

/// <summary>
/// The answer of the player who has the hand, judged by the game master: a correct one wins the points and reveals the
/// answer, a wrong one blocks the player and opens the buzzer anew to the others. Then the reveal, and the next question.
/// </summary>
public sealed class BuzzerJudgementTests
{
    private static readonly string[] _players = ["Zoé", "Max", "Léa"];

    private static readonly BuzzerRoundDescriptor _round =
        BuzzerGames.Round(BuzzerGames.PaintingQuestion, BuzzerGames.IllustratedQuestion, BuzzerGames.LastQuestion) with { Points = 500 };

    private static GameState NewGame => BuzzerGames.Started(_round, _players);

    [Fact]
    public void Handle_CorrectAnswer_AwardsThePointsAndRevealsTheAnswer()
    {
        // Given
        var state = BuzzerGames.Answering(NewGame, (2, 10));

        // When
        var transition = BuzzerGames.Engine.Handle(state, BuzzerGames.Judge(state, correct: true), Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        var round = BuzzerGames.RoundOf(transition.State);
        Assert.Equal((BuzzerPhase.Revealed, Games.PlayerIdOf(2)), (round.Phase, round.FoundBy));
        Assert.Equal([0, 500, 0], transition.State.Players.Select(player => player.Score));
    }

    [Fact]
    public void Handle_WrongAnswer_BlocksThePlayerAndReopensTheBuzzer()
    {
        // Given
        var state = BuzzerGames.Answering(NewGame, (2, 10));

        // When
        var transition = BuzzerGames.Engine.Handle(state, BuzzerGames.Judge(state, correct: false), Games.Context());

        // Then: no points lost, and a new opening
        Assert.Null(transition.Rejection);
        var round = BuzzerGames.RoundOf(transition.State);
        Assert.Equal((BuzzerPhase.Open, 2), (round.Phase, round.Buzzer.Opening));
        Assert.Equal([Games.PlayerIdOf(2)], round.Buzzer.Blocked);
        Assert.All(transition.State.Players, player => Assert.Equal(0, player.Score));
    }

    [Fact]
    public void Handle_WrongThenCorrectAnswer_AwardsTheSecondPlayer()
    {
        // When
        var state = BuzzerGames.Judged(NewGame, correct: true, 2, 3);

        // Then
        Assert.Equal(Games.PlayerIdOf(3), BuzzerGames.RoundOf(state).FoundBy);
        Assert.Equal([0, 0, 500], state.Players.Select(player => player.Score));
    }

    [Fact]
    public void Handle_LastConnectedPlayerRefused_LeavesTheBuzzerClosed()
    {
        // Given: Léa left, Zoé was refused, Max has the hand
        var state = BuzzerGames.Judged(Disconnected(NewGame, "Léa"), correct: false, 1);
        state = BuzzerGames.Accepted(state, BuzzerGames.Buzz(state, 2));
        state = BuzzerGames.Accepted(state, BuzzerGames.ArbitrationElapsed(state));

        // When
        var transition = BuzzerGames.Engine.Handle(state, BuzzerGames.Judge(state, correct: false), Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        var round = BuzzerGames.RoundOf(transition.State);
        Assert.Equal(BuzzerPhase.Closed, round.Phase);
        Assert.False(round.Buzzer.IsOpen);
    }

    [Fact]
    public void Handle_BuzzOnceEveryConnectedPlayerIsBlocked_IsRejected()
    {
        // Given: everybody but Léa, who left, was refused
        var state = BuzzerGames.Judged(Disconnected(NewGame, "Léa"), correct: false, 1, 2);

        // When
        var transition = BuzzerGames.Engine.Handle(state, BuzzerGames.Buzz(state, 3), Games.Context());

        // Then
        Assert.Equal(RejectionReason.BuzzerClosed, transition.Rejection);
    }

    [Fact]
    public void Handle_SameJudgmentTwice_IsRejectedAsObsolete()
    {
        // Given: Zoé refused, the buzzer reopened, and Max has the hand
        var state = BuzzerGames.Answering(NewGame, (1, 10));
        var judgment = BuzzerGames.Judge(state, correct: false);
        state = BuzzerGames.Accepted(state, judgment);
        state = BuzzerGames.Accepted(state, BuzzerGames.Buzz(state, 2));
        state = BuzzerGames.Accepted(state, BuzzerGames.ArbitrationElapsed(state));

        // When: the refusal of Zoé arrives again
        var transition = BuzzerGames.Engine.Handle(state, judgment, Games.Context());

        // Then: Max is not blocked
        Assert.Equal(RejectionReason.BuzzerOpeningMismatch, transition.Rejection);
        Assert.Same(state, transition.State);
    }

    [Fact]
    public void Handle_CorrectJudgmentTwice_AwardsThePointsOnce()
    {
        // Given
        var state = BuzzerGames.Answering(NewGame, (1, 10));
        var judgment = BuzzerGames.Judge(state, correct: true);
        state = BuzzerGames.Accepted(state, judgment);

        // When
        var transition = BuzzerGames.Engine.Handle(state, judgment, Games.Context());

        // Then
        Assert.Equal(RejectionReason.PhaseMismatch, transition.Rejection);
        Assert.Equal(500, transition.State.Players[0].Score);
    }

    [Fact]
    public void Handle_JudgmentWithoutWinner_IsRejected()
    {
        // Given
        var state = BuzzerGames.Buzzed(NewGame, (1, 10));

        // When
        var transition = BuzzerGames.Engine.Handle(state, BuzzerGames.Judge(state, correct: true), Games.Context());

        // Then
        Assert.Equal(RejectionReason.PhaseMismatch, transition.Rejection);
        Assert.Same(state, transition.State);
    }

    [Fact]
    public void Handle_JudgmentOfAnotherQuestion_IsRejected()
    {
        // Given
        var state = BuzzerGames.Answering(NewGame, (1, 10));

        // When
        var transition = BuzzerGames.Engine.Handle(state, BuzzerGames.Judge(state, correct: true, questionNumber: 2), Games.Context());

        // Then
        Assert.Equal(RejectionReason.QuestionMismatch, transition.Rejection);
    }

    [Fact]
    public void Handle_RevealWhileTheWinnerHasTheHand_RevealsWithoutPoints()
    {
        // Given
        var state = BuzzerGames.Answering(NewGame, (1, 10));

        // When
        var transition = BuzzerGames.Engine.Handle(state, BuzzerGames.RevealAnswer(state), Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        var round = BuzzerGames.RoundOf(transition.State);
        Assert.Equal((BuzzerPhase.Revealed, (Contracts.PlayerId?)null), (round.Phase, round.FoundBy));
        Assert.All(transition.State.Players, player => Assert.Equal(0, player.Score));
    }

    [Fact]
    public void Handle_RevealDuringTheArbitration_AbandonsIt()
    {
        // Given
        var state = BuzzerGames.Buzzed(NewGame, (1, 10));
        var timer = BuzzerGames.ArbitrationElapsed(state);

        // When
        var transition = BuzzerGames.Engine.Handle(state, BuzzerGames.RevealAnswer(state), Games.Context());

        // Then: the window is cancelled, and its timer, already sent, is obsolete
        Assert.Null(transition.Rejection);
        Assert.Equal(new CancelTimer(EngineBuzzer.ArbitrationTimer), Assert.Single(transition.Effects));
        Assert.Equal(RejectionReason.UnexpectedTimer, BuzzerGames.Engine.Handle(transition.State, timer, Games.Context()).Rejection);
    }

    [Theory]
    [InlineData(BuzzerPhase.Open)]
    [InlineData(BuzzerPhase.Closed)]
    public void Handle_RevealOfAQuestionAsked_RevealsIt(BuzzerPhase phase)
    {
        // Given
        var state = phase == BuzzerPhase.Open ? BuzzerGames.Asked(NewGame) : BuzzerGames.Judged(NewGame, correct: false, 1, 2, 3);
        Assert.Equal(phase, BuzzerGames.RoundOf(state).Phase);

        // When
        var transition = BuzzerGames.Engine.Handle(state, BuzzerGames.RevealAnswer(state), Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        Assert.Equal(BuzzerPhase.Revealed, BuzzerGames.RoundOf(transition.State).Phase);
    }

    [Fact]
    public void Handle_RevealBeforeTheQuestionIsAsked_IsRejected()
    {
        // Given
        var state = NewGame;

        // When
        var transition = BuzzerGames.Engine.Handle(state, BuzzerGames.RevealAnswer(state), Games.Context());

        // Then
        Assert.Equal(RejectionReason.PhaseMismatch, transition.Rejection);
    }

    [Fact]
    public void Handle_RevealOfAQuestionAlreadyRevealed_IsRejected()
    {
        // Given
        var state = BuzzerGames.Judged(NewGame, correct: true, 1);

        // When
        var transition = BuzzerGames.Engine.Handle(state, BuzzerGames.RevealAnswer(state), Games.Context());

        // Then
        Assert.Equal(RejectionReason.PhaseMismatch, transition.Rejection);
        Assert.Same(state, transition.State);
    }

    [Fact]
    public void Handle_BuzzOnceRevealed_IsRejected()
    {
        // Given
        var state = BuzzerGames.Accepted(BuzzerGames.Asked(NewGame), BuzzerGames.RevealAnswer(BuzzerGames.Asked(NewGame)));

        // When
        var transition = BuzzerGames.Engine.Handle(state, BuzzerGames.Buzz(state, 1), Games.Context());

        // Then
        Assert.Equal(RejectionReason.BuzzerClosed, transition.Rejection);
    }

    [Fact]
    public void Handle_NextQuestion_AnnouncesItWithAFreshBuzzer()
    {
        // Given: Zoé was blocked on the first question
        var state = BuzzerGames.Judged(NewGame, correct: true, 1, 2);

        // When
        var transition = BuzzerGames.Engine.Handle(state, BuzzerGames.NextQuestion(state), Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        var round = BuzzerGames.RoundOf(transition.State);
        Assert.Equal((2, BuzzerPhase.Ready), (round.QuestionNumber, round.Phase));
        Assert.Empty(round.Buzzer.Blocked);
        Assert.Null(round.FoundBy);
    }

    [Fact]
    public void Handle_NextQuestionBeforeTheReveal_IsRejected()
    {
        // Given
        var state = BuzzerGames.Answering(NewGame, (1, 10));

        // When
        var transition = BuzzerGames.Engine.Handle(state, BuzzerGames.NextQuestion(state), Games.Context());

        // Then
        Assert.Equal(RejectionReason.PhaseMismatch, transition.Rejection);
    }

    [Fact]
    public void Handle_NextQuestionTwice_IsRejectedAsObsolete()
    {
        // Given
        var revealed = BuzzerGames.Judged(NewGame, correct: true, 1);
        var next = BuzzerGames.NextQuestion(revealed);
        var state = BuzzerGames.Accepted(revealed, next);

        // When
        var transition = BuzzerGames.Engine.Handle(state, next, Games.Context());

        // Then
        Assert.Equal(RejectionReason.QuestionMismatch, transition.Rejection);
    }

    [Fact]
    public void Handle_NextQuestionAfterTheLastQuestion_EndsTheRound()
    {
        // Given
        var state = BuzzerGames.Judged(BuzzerGames.AtQuestion(NewGame, 2), correct: true, 1);

        // When
        var transition = BuzzerGames.Engine.Handle(state, BuzzerGames.NextQuestion(state), Games.Context());

        // Then: the pack has this round alone
        Assert.Null(transition.Rejection);
        Assert.Equal(GamePhase.Finished, transition.State.Phase);
        Assert.Equal(500, transition.State.Players[0].Score);
    }

    [Fact]
    public void ProjectForPlayer_Revealed_ShowsThePointsOfEachPlayer()
    {
        // Given: Zoé was refused, Max found
        var state = BuzzerGames.Judged(NewGame, correct: true, 1, 2);

        // Then
        var views = state.Players.Select(player => Assert.IsType<BuzzerPlayerView>(BuzzerGames.Snapshots.ForPlayer(state, player).RoundView));
        Assert.Equal([(BuzzerButtonState.Closed, 0), (BuzzerButtonState.Closed, 500), (BuzzerButtonState.Closed, 0)], views.Select(view => (view.Buzzer, view.Points!.Value)));
    }

    [Fact]
    public void ProjectForPlayer_WrongAnswer_ShowsThePlayerBlocked()
    {
        // Given
        var state = BuzzerGames.Judged(NewGame, correct: false, 1);

        // Then
        var view = Assert.IsType<BuzzerPlayerView>(BuzzerGames.Snapshots.ForPlayer(state, state.Players[0]).RoundView);
        Assert.Equal((BuzzerButtonState.Blocked, (int?)null), (view.Buzzer, view.Points));
    }

    [Fact]
    public void ProjectForDisplay_Revealed_ShowsTheAnswerAndWhoFoundIt()
    {
        // Given
        var state = BuzzerGames.Judged(NewGame, correct: true, 1, 2);

        // When
        var view = Assert.IsType<BuzzerDisplayView>(BuzzerGames.Snapshots.ForDisplay(state).RoundView);

        // Then
        Assert.Equal(
            (BuzzerQuestionPhase.Revealed, BuzzerGames.PaintingQuestion.Answer, "Max", (string?)null),
            (view.Phase, view.Answer, view.FoundBy, view.Winner));
    }

    private static GameState Disconnected(GameState state, string nickname) =>
        state with { Players = [.. state.Players.Select(player => player.Nickname == nickname ? player with { IsConnected = false } : player)] };
}
