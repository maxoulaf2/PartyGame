using PartyGame.Contracts;
using PartyGame.Engine.Effects;
using PartyGame.Engine.Inputs;
using PartyGame.Engine.State;

namespace PartyGame.Engine.Tests.Rounds;

public sealed class SkipRoundTests
{
    public static TheoryData<GamePhase> OutsideRound => [GamePhase.Lobby, GamePhase.BetweenRounds, GamePhase.Finished];

    [Fact]
    public void Handle_SkipRoundInTheMiddleOfARound_GoesBetweenRoundsWithoutTheMode()
    {
        // Given: a round in the middle of its play
        var state = Games.InPhase(GamePhase.Round, "Zoé", "Max");
        state = Games.Accepted(state, Games.GameMasterActs(state, "reveals"));

        // When
        var transition = Games.Engine.Handle(state, Games.SkipRound(state), Games.Context());

        // Then: the round ends as the mode last left it, which never hears of the skip
        Assert.Null(transition.Rejection);
        Assert.Equal(GamePhase.BetweenRounds, transition.State.Phase);
        var round = transition.State.CurrentRound!;
        Assert.Equal((state.CurrentRound!.Id, 0, true), (round.Id, round.Index, round.IsSkipped));
        Assert.Same(state.CurrentRound.State, round.State);
    }

    [Fact]
    public void Handle_SkipRound_CancelsTheTimersOfTheRound()
    {
        // Given: the round scheduled a countdown when it started
        var state = Games.InPhase(GamePhase.Round, "Zoé");

        // When
        var transition = Games.Engine.Handle(state, Games.SkipRound(state), Games.Context());

        // Then
        Assert.Equal([new CancelRoundTimers(state.CurrentRound!.Id)], transition.Effects);
    }

    [Fact]
    public void Handle_SkipLastRound_FinishesTheGame()
    {
        // Given
        var state = Games.InLastRound("Zoé");

        // When
        var transition = Games.Engine.Handle(state, Games.SkipRound(state), Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        Assert.Equal(GamePhase.Finished, transition.State.Phase);
        Assert.Equal((1, true), (transition.State.CurrentRound!.Index, transition.State.CurrentRound.IsSkipped));
    }

    [Fact]
    public void Handle_SkipRound_KeepsThePointsAlreadyAwarded()
    {
        // Given: the round awarded points, then a player answered the question in progress
        var state = Games.InPhase(GamePhase.Round, "Zoé", "Max");
        state = Games.Accepted(state, Games.GameMasterActs(state, FakeGameMasterIntent.Award));
        state = Games.Accepted(state, Games.PlayerActs(state, player: 1, "answers A"));

        // When
        var skipped = Games.Accepted(state, Games.SkipRound(state));

        // Then: what the question in progress would award is never awarded
        Assert.Equal([FakeMode.AwardedPoints, FakeMode.AwardedPoints], skipped.Players.Select(p => p.Score));
        Assert.Equal(state.Players, skipped.Players);
    }

    [Fact]
    public void Handle_TimerOfASkippedRound_IsRejected()
    {
        // Given: the countdown of the skipped round elapsed before its cancellation
        var state = Games.InPhase(GamePhase.Round, "Zoé");
        var timer = new TimerElapsed(FakeMode.Countdown, Games.Now + FakeMode.CountdownDuration) { RoundId = state.CurrentRound!.Id };
        var skipped = Games.Accepted(state, Games.SkipRound(state));

        // When
        var transition = Games.Engine.Handle(skipped, timer, Games.Context());

        // Then
        Assert.Same(skipped, transition.State);
        Assert.Equal(RejectionReason.UnexpectedTimer, transition.Rejection);
    }

    [Fact]
    public void Handle_NextRoundAfterASkippedRound_PlaysItNormally()
    {
        // Given
        var state = Games.InPhase(GamePhase.Round, "Zoé");
        state = Games.Accepted(state, Games.SkipRound(state));

        // When
        var next = Games.NextRoundStarted(state);

        // Then
        Assert.Equal(GamePhase.Round, next.Phase);
        Assert.Equal((1, false), (next.CurrentRound!.Index, next.CurrentRound.IsSkipped));
        Assert.Equal(["start with 1 players"], ((FakeRoundState)next.CurrentRound.State!).Inputs);
    }

    [Fact]
    public void Handle_SkipRoundTwice_RejectsTheSecond()
    {
        // Given: a double tap, a second console, or a request sent again after a reconnection
        var state = Games.InPhase(GamePhase.Round, "Zoé");
        var skip = Games.SkipRound(state);
        var skipped = Games.Accepted(state, skip);

        // When
        var transition = Games.Engine.Handle(skipped, skip, Games.Context());

        // Then
        Assert.Same(skipped, transition.State);
        Assert.Empty(transition.Effects);
        Assert.Equal(RejectionReason.NotInRound, transition.Rejection);
    }

    [Fact]
    public void Handle_SkipRoundNamingAPreviousRound_IsRejected()
    {
        // Given: a skip of the first round, delayed until the second one started
        var first = Games.InPhase(GamePhase.Round, "Zoé");
        var state = Games.Accepted(first, Games.GameMasterActs(first, FakeGameMasterIntent.Finish));
        state = Games.NextRoundStarted(state);

        // When
        var transition = Games.Engine.Handle(state, Games.SkipRound(first), Games.Context());

        // Then
        Assert.Same(state, transition.State);
        Assert.Empty(transition.Effects);
        Assert.Equal(RejectionReason.RoundMismatch, transition.Rejection);
    }

    [Theory]
    [MemberData(nameof(OutsideRound))]
    public void Handle_SkipRoundOutsideRound_IsRejected(GamePhase phase)
    {
        // Given: between two rounds, even a skip naming the round that just finished
        var state = Games.InPhase(phase, "Zoé");
        var roundId = state.CurrentRound?.Id ?? new RoundId(Guid.NewGuid());

        // When
        var transition = Games.Engine.Handle(state, new SkipRound(roundId, Games.Now), Games.Context());

        // Then
        Assert.Same(state, transition.State);
        Assert.Empty(transition.Effects);
        Assert.Equal(RejectionReason.NotInRound, transition.Rejection);
    }
}
