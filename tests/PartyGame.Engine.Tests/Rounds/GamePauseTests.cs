using PartyGame.Contracts;
using PartyGame.Engine.Effects;
using PartyGame.Engine.Inputs;
using PartyGame.Engine.State;

namespace PartyGame.Engine.Tests.Rounds;

/// <summary>
/// Pause of the game by the game master: everything stands still until they resume it, and the round goes on with the time
/// it had left.
/// </summary>
public sealed class GamePauseTests
{
    /// <summary>The game master resumes the game 5 minutes after pausing it.</summary>
    private static readonly DateTimeOffset _resumedAt = Games.Now.AddMinutes(5);

    public static TheoryData<GamePhase> PausablePhases => [GamePhase.RoundIntro, GamePhase.Round, GamePhase.BetweenRounds];

    public static TheoryData<GamePhase> UnpausablePhases => [GamePhase.Lobby, GamePhase.Finished, GamePhase.ResumePending];

    public static TheoryData<string> RoundInputs =>
        ["player intent", "game master intent", "timer", "next round", "start round", "skip round"];

    [Theory]
    [MemberData(nameof(PausablePhases))]
    public void Handle_PauseOnceStarted_PausesTheGameInTheSamePhase(GamePhase phase)
    {
        // Given
        var state = Games.InPhase(phase, "Zoé", "Max");

        // When
        var transition = Games.Engine.Handle(state, Pause(state), Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        Assert.Equal(state with { PausedAt = Games.Now }, transition.State);
    }

    [Fact]
    public void Handle_PauseDuringARound_CancelsItsTimers()
    {
        // Given
        var state = Games.InPhase(GamePhase.Round, "Zoé");

        // When
        var transition = Games.Engine.Handle(state, Pause(state), Games.Context());

        // Then
        Assert.Equal([new CancelRoundTimers(state.CurrentRound!.Id)], transition.Effects);
    }

    [Theory]
    [MemberData(nameof(UnpausablePhases))]
    public void Handle_PauseOutOfAGame_IsRejected(GamePhase phase)
    {
        // Given
        var state = Games.InPhase(phase, "Zoé");

        // When
        var transition = Games.Engine.Handle(state, Pause(state), Games.Context());

        // Then
        Assert.NotNull(transition.Rejection);
        Assert.Same(state, transition.State);
    }

    [Fact]
    public void Handle_PauseOfAnotherGame_IsRejected()
    {
        // Given
        var state = Games.InPhase(GamePhase.Round, "Zoé");

        // When
        var transition = Games.Engine.Handle(state, new PauseGame(new GameId(Guid.NewGuid()), Paused: true, Games.Now), Games.Context());

        // Then
        Assert.Equal(RejectionReason.GameMismatch, transition.Rejection);
    }

    [Fact]
    public void Handle_PauseTwice_SecondIsRejectedAsObsolete()
    {
        // Given
        var state = Paused(Games.InPhase(GamePhase.Round, "Zoé"));

        // When
        var transition = Games.Engine.Handle(state, Pause(state), Games.Context());

        // Then
        Assert.Equal(RejectionReason.PauseUnchanged, transition.Rejection);
    }

    [Fact]
    public void Handle_ResumeTwice_SecondIsRejectedAsObsolete()
    {
        // Given
        var state = Games.InPhase(GamePhase.Round, "Zoé");

        // When
        var transition = Games.Engine.Handle(state, Resume(state), Games.Context());

        // Then
        Assert.Equal(RejectionReason.PauseUnchanged, transition.Rejection);
    }

    [Fact]
    public void Handle_ResumeDuringARound_MovesItsDeadlinesOnByThePauseAndSchedulesItsTimersAgain()
    {
        // Given: the countdown of 20 s started at Now, paused 8 s in
        var started = Games.InPhase(GamePhase.Round, "Zoé");
        var state = Games.Engine.Handle(started, Pause(started), At(Games.Now.AddSeconds(8))).State;

        // When
        var transition = Games.Engine.Handle(state, Resume(state), At(_resumedAt));

        // Then: 12 s left
        Assert.Null(transition.Rejection);
        Assert.Null(transition.State.PausedAt);
        var dueAt = _resumedAt.AddSeconds(12);
        Assert.Equal(dueAt, CountdownOf(transition.State));
        Assert.Equal(
            [new ScheduleTimer(FakeMode.Countdown, dueAt) { RoundId = state.CurrentRound!.Id }],
            transition.Effects);
    }

    [Theory]
    [InlineData(GamePhase.RoundIntro)]
    [InlineData(GamePhase.BetweenRounds)]
    public void Handle_ResumeOutOfARound_GoesOnWithoutTimer(GamePhase phase)
    {
        // Given
        var state = Paused(Games.InPhase(phase, "Zoé"));

        // When
        var transition = Games.Engine.Handle(state, Resume(state), At(_resumedAt));

        // Then
        Assert.Equal(state with { PausedAt = null }, transition.State);
        Assert.Empty(transition.Effects);
    }

    [Theory]
    [MemberData(nameof(RoundInputs))]
    public void Handle_RoundInputWhilePaused_IsRejected(string input)
    {
        // Given
        var state = Paused(Games.InPhase(input is "next round" ? GamePhase.BetweenRounds : input is "start round" ? GamePhase.RoundIntro : GamePhase.Round, "Zoé"));

        // When
        GameInput sent = input switch
        {
            "player intent" => Games.PlayerActs(state, 1, "answer"),
            "game master intent" => Games.GameMasterActs(state, "reveal"),
            "timer" => new TimerElapsed(FakeMode.Countdown, CountdownOf(state)) { RoundId = state.CurrentRound!.Id },
            "next round" => Games.NextRound(state),
            "start round" => Games.StartRound(state),
            _ => Games.SkipRound(state),
        };
        var transition = Games.Engine.Handle(state, sent, Games.Context());

        // Then
        Assert.Equal(RejectionReason.GamePaused, transition.Rejection);
        Assert.Same(state, transition.State);
    }

    [Fact]
    public void Handle_RenameOrJoinWhilePaused_IsAccepted()
    {
        // Given
        var state = Paused(Games.InPhase(GamePhase.Round, "Zoé"));

        // When
        state = Games.Accepted(state, Games.Rename(1, "Zoë"));
        state = Games.Accepted(state, Games.Join("Max", player: 2));

        // Then: still paused
        Assert.Equal(["Zoë", "Max"], state.Players.Select(p => p.Nickname));
        Assert.Equal(Games.Now, state.PausedAt);
    }

    [Fact]
    public void Handle_ReturnToLobbyWhilePaused_StartsANewGameNotPaused()
    {
        // Given
        var state = Paused(Games.InPhase(GamePhase.Round, "Zoé"));

        // When
        var lobby = Games.Accepted(state, Games.ReturnToLobby(state));

        // Then
        Assert.Equal(GamePhase.Lobby, lobby.Phase);
        Assert.Null(lobby.PausedAt);
    }

    [Fact]
    public void Handle_GameSavedPausedResumedAfterARestart_StaysPausedWithTheSameTimeLeft()
    {
        // Given: paused 8 s into the countdown, the server restarted, the game resumed by the game master
        var started = Games.InPhase(GamePhase.Round, "Zoé");
        var saved = Games.Engine.Handle(started, Pause(started), At(Games.Now.AddSeconds(8))).State;
        var restored = Games.Accepted(Games.Pending(saved), Games.Resume(Games.Pending(saved)));

        // When: the server catches up with the time spent offline
        var caughtUp = Games.Engine.Handle(restored, new GameResumed(Games.Now.AddMinutes(1)), At(Games.Now.AddMinutes(3)));

        // Then: nothing moves until the game master resumes the game
        Assert.Equal(restored, caughtUp.State);
        Assert.Empty(caughtUp.Effects);
        var resumed = Games.Engine.Handle(caughtUp.State, Resume(caughtUp.State), At(_resumedAt));
        Assert.Equal(_resumedAt.AddSeconds(12), CountdownOf(resumed.State));
    }

    private static PauseGame Pause(GameState state) => new(state.GameId, Paused: true, Games.Now);

    private static PauseGame Resume(GameState state) => new(state.GameId, Paused: false, Games.Now);

    private static GameState Paused(GameState state) => Games.Accepted(state, Pause(state));

    private static DateTimeOffset CountdownOf(GameState state) =>
        Assert.IsType<FakeRoundState>(state.CurrentRound!.State).CountdownDueAt;

    private static GameContext At(DateTimeOffset now) => new(now, new Random(42));
}
