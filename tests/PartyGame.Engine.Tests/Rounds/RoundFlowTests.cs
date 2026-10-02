using PartyGame.Contracts;
using PartyGame.Contracts.Packs;
using PartyGame.Engine.Effects;
using PartyGame.Engine.Inputs;

namespace PartyGame.Engine.Tests.Rounds;

public sealed class RoundFlowTests
{
    public static TheoryData<GamePhase> OutsideRound => [GamePhase.Lobby, GamePhase.BetweenRounds, GamePhase.Finished];

    public static TheoryData<GamePhase> NotBetweenRounds => [GamePhase.Lobby, GamePhase.Round, GamePhase.Finished];

    [Fact]
    public void Handle_StartGameWithRounds_StartsTheFirstRoundWithItsMode()
    {
        // Given
        var state = Games.InPhase(GamePhase.Lobby, "Zoé", "Max");

        // When
        var transition = Games.Engine.Handle(state, Games.Start(), Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        var started = transition.State;
        Assert.Equal(GamePhase.Round, started.Phase);
        var round = Assert.IsType<PlayedRound>(started.CurrentRound);
        Assert.Equal(0, round.Index);
        var roundState = Assert.IsType<FakeRoundState>(round.State);
        Assert.Equal(("Échauffement", Games.Now + FakeMode.CountdownDuration), (roundState.Title, roundState.CountdownDueAt));
        Assert.Equal(["start with 2 players"], roundState.Inputs);
        Assert.Equal(state.Players, started.Players);
    }

    [Fact]
    public void Handle_StartGameWithRounds_MarksTheTimersOfTheModeWithTheRound()
    {
        // Given
        var state = Games.InPhase(GamePhase.Lobby, "Zoé");

        // When
        var transition = Games.Engine.Handle(state, Games.Start(), Games.Context());

        // Then
        var timer = Assert.IsType<ScheduleTimer>(Assert.Single(transition.Effects));
        Assert.Equal(
            new ScheduleTimer(FakeMode.Countdown, Games.Now + FakeMode.CountdownDuration) { RoundId = transition.State.CurrentRound!.Id },
            timer);
    }

    [Fact]
    public void Handle_StartGameWithSameSeed_GivesTheRoundTheSameIdentifier()
    {
        // Given
        var state = Games.InPhase(GamePhase.Lobby, "Zoé");

        // When
        var first = Games.Engine.Handle(state, Games.Start(), Games.Context(seed: 7)).State.CurrentRound!.Id;
        var second = Games.Engine.Handle(state, Games.Start(), Games.Context(seed: 7)).State.CurrentRound!.Id;

        // Then
        Assert.Equal(first, second);
        Assert.NotEqual(Guid.Empty, first.Value);
    }

    [Fact]
    public void Handle_StartGameWithoutRounds_FinishesTheGame()
    {
        // Given: no pack chosen yet
        var state = Games.LobbyWith("Zoé");

        // When
        var transition = Games.Engine.Handle(state, Games.Start(), Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        Assert.Empty(transition.Effects);
        Assert.Equal(state with { Phase = GamePhase.Finished }, transition.State);
    }

    [Fact]
    public void Handle_StartGameWithAnActivityWithoutMode_IsRejected()
    {
        // Given: no mode plays quiz rounds in these tests
        var state = Games.LobbyWith("Zoé") with { Rounds = [Games.TwoRounds[0], new QuizRoundDescriptor { Title = "Quiz" }] };

        // When
        var transition = Games.Engine.Handle(state, Games.Start(), Games.Context());

        // Then
        Assert.Same(state, transition.State);
        Assert.Empty(transition.Effects);
        Assert.Equal(RejectionReason.GameModeMissing, transition.Rejection);
    }

    [Fact]
    public void Handle_PlayerRoundIntentInRound_IsHandedToTheModeWithTheRoundState()
    {
        // Given
        var state = Games.InPhase(GamePhase.Round, "Zoé", "Max");

        // When
        var transition = Games.Engine.Handle(state, Games.PlayerActs(state, player: 2, "answers B"), Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        Assert.Equal(GamePhase.Round, transition.State.Phase);
        Assert.Equal(state.CurrentRound!.Id, transition.State.CurrentRound!.Id);
        Assert.Equal(
            ["start with 2 players", $"player {Games.PlayerIdOf(2).Value} answers B"],
            ((FakeRoundState)transition.State.CurrentRound.State).Inputs);
    }

    [Fact]
    public void Handle_GameMasterRoundIntentInRound_IsHandedToTheMode()
    {
        // Given
        var state = Games.InPhase(GamePhase.Round, "Zoé");

        // When
        var transition = Games.Engine.Handle(state, Games.GameMasterActs(state, "reveals"), Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        Assert.Equal(["start with 1 players", "game master reveals"], ((FakeRoundState)transition.State.CurrentRound!.State).Inputs);
    }

    [Fact]
    public void Handle_RoundIntentTheModeAcceptsWithoutChange_KeepsTheSameState()
    {
        // Given
        var state = Games.InPhase(GamePhase.Round, "Zoé");

        // When
        var transition = Games.Engine.Handle(state, Games.GameMasterActs(state, FakeGameMasterIntent.Nothing), Games.Context());

        // Then: the same instance tells the loop that there is nothing to broadcast
        Assert.Null(transition.Rejection);
        Assert.Same(state, transition.State);
    }

    [Theory]
    [MemberData(nameof(OutsideRound))]
    public void Handle_PlayerRoundIntentOutsideRound_IsRejected(GamePhase phase)
    {
        // Given
        var state = Games.InPhase(phase, "Zoé");
        var intent = new PlayerRoundInput(Games.PlayerIdOf(1), new FakePlayerIntent(new RoundId(Guid.NewGuid()), "answers A"), Games.Now);

        // When
        var transition = Games.Engine.Handle(state, intent, Games.Context());

        // Then
        Assert.Same(state, transition.State);
        Assert.Empty(transition.Effects);
        Assert.Equal(RejectionReason.NotInRound, transition.Rejection);
    }

    [Theory]
    [MemberData(nameof(OutsideRound))]
    public void Handle_GameMasterRoundIntentOutsideRound_IsRejected(GamePhase phase)
    {
        // Given: between two rounds, even an intent naming the round that just finished
        var state = Games.InPhase(phase, "Zoé");
        var roundId = state.CurrentRound?.Id ?? new RoundId(Guid.NewGuid());

        // When
        var transition = Games.Engine.Handle(state, new GameMasterRoundInput(new FakeGameMasterIntent(roundId, "reveals"), Games.Now), Games.Context());

        // Then
        Assert.Same(state, transition.State);
        Assert.Empty(transition.Effects);
        Assert.Equal(RejectionReason.NotInRound, transition.Rejection);
    }

    [Fact]
    public void Handle_RoundIntentNamingAnotherRound_IsRejected()
    {
        // Given: an intent of the first round, sent again once the second one started
        var first = Games.InPhase(GamePhase.Round, "Zoé");
        var state = Games.Accepted(first, Games.GameMasterActs(first, FakeGameMasterIntent.Finish));
        state = Games.Accepted(state, Games.NextRound(state), seed: 43);

        // When
        Transition[] transitions =
        [
            Games.Engine.Handle(state, Games.PlayerActs(first, player: 1, "answers A"), Games.Context()),
            Games.Engine.Handle(state, Games.GameMasterActs(first, "reveals"), Games.Context()),
        ];

        // Then
        Assert.All(transitions, transition =>
        {
            Assert.Same(state, transition.State);
            Assert.Empty(transition.Effects);
            Assert.Equal(RejectionReason.RoundMismatch, transition.Rejection);
        });
    }

    [Fact]
    public void Handle_PlayerRoundIntentFromUnknownPlayer_IsRejected()
    {
        // Given
        var state = Games.InPhase(GamePhase.Round, "Zoé");

        // When
        var transition = Games.Engine.Handle(state, Games.PlayerActs(state, player: 9, "answers A"), Games.Context());

        // Then
        Assert.Same(state, transition.State);
        Assert.Equal(RejectionReason.PlayerUnknown, transition.Rejection);
    }

    [Fact]
    public void Handle_RoundFinishedBeforeTheLast_GoesBetweenRounds()
    {
        // Given
        var state = Games.InPhase(GamePhase.Round, "Zoé");

        // When
        var transition = Games.Engine.Handle(state, Games.GameMasterActs(state, FakeGameMasterIntent.Finish), Games.Context());

        // Then: the round that just finished stays known, for the game master to name it
        Assert.Null(transition.Rejection);
        Assert.Equal(GamePhase.BetweenRounds, transition.State.Phase);
        Assert.Equal(state.CurrentRound, transition.State.CurrentRound);
    }

    [Fact]
    public void Handle_LastRoundFinished_FinishesTheGame()
    {
        // Given
        var state = Games.InPhase(GamePhase.BetweenRounds, "Zoé");
        state = Games.Accepted(state, Games.NextRound(state), seed: 43);

        // When
        var transition = Games.Engine.Handle(state, Games.GameMasterActs(state, FakeGameMasterIntent.Finish), Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        Assert.Equal(GamePhase.Finished, transition.State.Phase);
        Assert.Equal(1, transition.State.CurrentRound!.Index);
    }

    [Fact]
    public void Handle_NextRoundAfterTheFinishedRound_StartsTheNextRound()
    {
        // Given
        var state = Games.InPhase(GamePhase.BetweenRounds, "Zoé", "Max");

        // When
        var transition = Games.Engine.Handle(state, Games.NextRound(state), Games.Context(seed: 43));

        // Then
        Assert.Null(transition.Rejection);
        Assert.Equal(GamePhase.Round, transition.State.Phase);
        var round = transition.State.CurrentRound!;
        Assert.Equal(1, round.Index);
        Assert.NotEqual(state.CurrentRound!.Id, round.Id);
        var roundState = Assert.IsType<FakeRoundState>(round.State);
        Assert.Equal("Finale", roundState.Title);
        Assert.Equal(["start with 2 players"], roundState.Inputs);
        var timer = Assert.IsType<ScheduleTimer>(Assert.Single(transition.Effects));
        Assert.Equal(round.Id, timer.RoundId);
    }

    [Fact]
    public void Handle_NextRoundTwice_RejectsTheSecond()
    {
        // Given: a double tap, a second console, or a request sent again after a reconnection
        var finished = Games.InPhase(GamePhase.BetweenRounds, "Zoé");
        var state = Games.Accepted(finished, Games.NextRound(finished), seed: 43);

        // When
        var transition = Games.Engine.Handle(state, Games.NextRound(finished), Games.Context(seed: 44));

        // Then
        Assert.Same(state, transition.State);
        Assert.Empty(transition.Effects);
        Assert.Equal(RejectionReason.NotBetweenRounds, transition.Rejection);
    }

    [Fact]
    public void Handle_NextRoundNamingAnotherRound_IsRejected()
    {
        // Given
        var state = Games.InPhase(GamePhase.BetweenRounds, "Zoé");

        // When
        var transition = Games.Engine.Handle(state, new NextRound(new RoundId(Guid.NewGuid()), Games.Now), Games.Context());

        // Then
        Assert.Same(state, transition.State);
        Assert.Empty(transition.Effects);
        Assert.Equal(RejectionReason.RoundMismatch, transition.Rejection);
    }

    [Theory]
    [MemberData(nameof(NotBetweenRounds))]
    public void Handle_NextRoundOutsideBetweenRounds_IsRejected(GamePhase phase)
    {
        // Given
        var state = Games.InPhase(phase, "Zoé");
        var afterRound = state.CurrentRound?.Id ?? new RoundId(Guid.NewGuid());

        // When
        var transition = Games.Engine.Handle(state, new NextRound(afterRound, Games.Now), Games.Context());

        // Then
        Assert.Same(state, transition.State);
        Assert.Empty(transition.Effects);
        Assert.Equal(RejectionReason.NotBetweenRounds, transition.Rejection);
    }

    [Fact]
    public void Handle_TimerOfTheRoundInProgress_IsHandedToTheMode()
    {
        // Given
        var state = Games.InPhase(GamePhase.Round, "Zoé");
        var timer = Elapsed(state.CurrentRound!.Id, Games.Now + FakeMode.CountdownDuration);

        // When
        var transition = Games.Engine.Handle(state, timer, Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        Assert.Equal(["start with 1 players", "timer fake-countdown"], ((FakeRoundState)transition.State.CurrentRound!.State).Inputs);
    }

    [Fact]
    public void Handle_TimerRejectedByTheMode_KeepsTheState()
    {
        // Given: a countdown the round replaced since
        var state = Games.InPhase(GamePhase.Round, "Zoé");
        var timer = Elapsed(state.CurrentRound!.Id, Games.Now);

        // When
        var transition = Games.Engine.Handle(state, timer, Games.Context());

        // Then
        Assert.Same(state, transition.State);
        Assert.Empty(transition.Effects);
        Assert.Equal(RejectionReason.UnexpectedTimer, transition.Rejection);
    }

    [Fact]
    public void Handle_TimerOfAFinishedRound_IsRejected()
    {
        // Given: the countdown of the first round elapses between the rounds, then during the second one
        var first = Games.InPhase(GamePhase.Round, "Zoé");
        var timer = Elapsed(first.CurrentRound!.Id, Games.Now + FakeMode.CountdownDuration);
        var betweenRounds = Games.Accepted(first, Games.GameMasterActs(first, FakeGameMasterIntent.Finish));
        var secondRound = Games.Accepted(betweenRounds, Games.NextRound(betweenRounds), seed: 43);

        // When
        Transition[] transitions =
        [
            Games.Engine.Handle(betweenRounds, timer, Games.Context()),
            Games.Engine.Handle(secondRound, timer, Games.Context()),
        ];

        // Then
        Assert.Same(betweenRounds, transitions[0].State);
        Assert.Same(secondRound, transitions[1].State);
        Assert.All(transitions, transition => Assert.Equal(RejectionReason.UnexpectedTimer, transition.Rejection));
    }

    [Fact]
    public void Handle_TimerWithoutRoundDuringARound_IsRejected()
    {
        // Given
        var state = Games.InPhase(GamePhase.Round, "Zoé");

        // When
        var transition = Games.Engine.Handle(state, new TimerElapsed(FakeMode.Countdown, Games.Now + FakeMode.CountdownDuration), Games.Context());

        // Then
        Assert.Same(state, transition.State);
        Assert.Equal(RejectionReason.UnexpectedTimer, transition.Rejection);
    }

    [Fact]
    public void Handle_JoinGameDuringARound_RegistersThePlayerAndKeepsTheRound()
    {
        // Given
        var state = Games.InPhase(GamePhase.Round, "Zoé");

        // When
        var transition = Games.Engine.Handle(state, Games.Join("Max", player: 2), Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        Assert.Equal(GamePhase.Round, transition.State.Phase);
        Assert.Same(state.CurrentRound, transition.State.CurrentRound);
        Assert.Equal(["Zoé", "Max"], transition.State.Players.Select(p => p.Nickname));
    }

    private static TimerElapsed Elapsed(RoundId roundId, DateTimeOffset dueAt) =>
        new(FakeMode.Countdown, dueAt) { RoundId = roundId };
}
