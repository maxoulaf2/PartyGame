using PartyGame.Contracts;
using PartyGame.Engine.Effects;
using PartyGame.Engine.Inputs;
using PartyGame.Engine.State;

namespace PartyGame.Engine.Tests.Rounds;

public sealed class RoundIntroTests
{
    public static TheoryData<GamePhase> NotInIntro => [GamePhase.Lobby, GamePhase.Round, GamePhase.BetweenRounds, GamePhase.Finished];

    [Fact]
    public void Handle_StartGame_AnnouncesTheFirstRoundWithoutItsMode()
    {
        // Given
        var state = Games.InPhase(GamePhase.Lobby, "Zoé");

        // When
        var transition = Games.Engine.Handle(state, Games.Start(), Games.Context());

        // Then: the mode starts nothing, not even a countdown
        Assert.Null(transition.Rejection);
        Assert.Equal(GamePhase.RoundIntro, transition.State.Phase);
        var round = transition.State.CurrentRound!;
        Assert.Equal((0, null), (round.Index, round.State));
        Assert.Empty(transition.Effects);
    }

    [Fact]
    public void Handle_StartRoundTwice_StartsTheRoundOnce()
    {
        // Given: a double tap, a second console, or a request sent again after a reconnection
        var announced = Games.InPhase(GamePhase.RoundIntro, "Zoé");
        var state = Games.Accepted(announced, Games.StartRound(announced));

        // When
        var transition = Games.Engine.Handle(state, Games.StartRound(announced), Games.Context());

        // Then
        Assert.Same(state, transition.State);
        Assert.Empty(transition.Effects);
        Assert.Equal(RejectionReason.NoRoundAnnounced, transition.Rejection);
    }

    [Fact]
    public void Handle_StartRoundNamingAnotherRound_IsRejected()
    {
        // Given
        var state = Games.InPhase(GamePhase.RoundIntro, "Zoé");

        // When
        var transition = Games.Engine.Handle(state, new StartRound(new RoundId(Guid.NewGuid()), Games.Now), Games.Context());

        // Then
        Assert.Same(state, transition.State);
        Assert.Empty(transition.Effects);
        Assert.Equal(RejectionReason.RoundMismatch, transition.Rejection);
    }

    [Theory]
    [MemberData(nameof(NotInIntro))]
    public void Handle_StartRoundOutsideAnIntroduction_IsRejected(GamePhase phase)
    {
        // Given: between two rounds, even a request naming the round that just finished
        var state = Games.InPhase(phase, "Zoé");
        var roundId = state.CurrentRound?.Id ?? new RoundId(Guid.NewGuid());

        // When
        var transition = Games.Engine.Handle(state, new StartRound(roundId, Games.Now), Games.Context());

        // Then
        Assert.Same(state, transition.State);
        Assert.Equal(RejectionReason.NoRoundAnnounced, transition.Rejection);
    }

    [Fact]
    public void Handle_RoundIntentDuringTheIntroduction_IsRejected()
    {
        // Given: the round announced has no state for its mode yet
        var state = Games.InPhase(GamePhase.RoundIntro, "Zoé");

        // When
        Transition[] transitions =
        [
            Games.Engine.Handle(state, Games.PlayerActs(state, player: 1, "answers A"), Games.Context()),
            Games.Engine.Handle(state, Games.GameMasterActs(state, "reveals"), Games.Context()),
            Games.Engine.Handle(state, new TimerElapsed(FakeMode.Countdown, Games.Now) { RoundId = state.CurrentRound!.Id }, Games.Context()),
        ];

        // Then
        Assert.All(transitions, transition => Assert.Same(state, transition.State));
    }

    [Fact]
    public void Handle_NextRoundDuringTheIntroduction_IsRejected()
    {
        // Given: the request that announced the second round, sent again
        var finished = Games.InPhase(GamePhase.BetweenRounds, "Zoé");
        var state = Games.Accepted(finished, Games.NextRound(finished), seed: 43);

        // When
        var transition = Games.Engine.Handle(state, Games.NextRound(finished), Games.Context(seed: 44));

        // Then
        Assert.Same(state, transition.State);
        Assert.Equal(RejectionReason.NotBetweenRounds, transition.Rejection);
    }

    [Fact]
    public void Handle_SkipRoundDuringTheIntroduction_SkipsItWithoutItsMode()
    {
        // Given: a round whose start keeps failing
        var state = Games.InPhase(GamePhase.RoundIntro, "Zoé");

        // When
        var transition = Games.Engine.Handle(state, Games.SkipRound(state), Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        Assert.Equal(GamePhase.BetweenRounds, transition.State.Phase);
        Assert.Equal((state.CurrentRound!.Id, true, null), (transition.State.CurrentRound!.Id, transition.State.CurrentRound.IsSkipped, transition.State.CurrentRound.State));
    }

    [Fact]
    public void Handle_ReturnToLobbyDuringTheIntroduction_GoesBackToTheLobby()
    {
        // Given
        var state = Games.InPhase(GamePhase.RoundIntro, "Zoé");

        // When
        var transition = Games.Engine.Handle(state, Games.ReturnToLobby(state), Games.Context());

        // Then
        Assert.Equal((GamePhase.Lobby, null), (transition.State.Phase, transition.State.CurrentRound));
        Assert.DoesNotContain(transition.Effects, effect => effect is CancelRoundTimers);
    }

    [Fact]
    public void Handle_GameResumedDuringTheIntroduction_KeepsTheIntroduction()
    {
        // Given: a server restarted while a round was announced
        var state = Games.InPhase(GamePhase.RoundIntro, "Zoé");

        // When
        var transition = Games.Engine.Handle(state, new GameResumed(Games.SavedAt), Games.Context());

        // Then
        Assert.Same(state, transition.State);
        Assert.Empty(transition.Effects);
    }
}
