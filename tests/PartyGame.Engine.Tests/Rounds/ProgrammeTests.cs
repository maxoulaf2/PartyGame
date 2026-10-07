using System.Collections.Immutable;
using System.Text.Json;
using PartyGame.Contracts;
using PartyGame.Engine.Inputs;
using PartyGame.Engine.Projections;
using PartyGame.Engine.State;

namespace PartyGame.Engine.Tests.Rounds;

/// <summary>
/// The programme of the rounds, which the game master reorders, withdraws from and puts back into while the game goes on:
/// the request names the programme it replaces.
/// </summary>
public sealed class ProgrammeTests
{
    private static readonly JsonSerializerOptions _options = GameStateJson.CreateOptions(Games.Modes, FakeJson.Options);

    public static TheoryData<GamePhase> ReorderablePhases => [GamePhase.RoundIntro, GamePhase.Round, GamePhase.BetweenRounds];

    [Fact]
    public void Handle_StartGame_SchedulesTheRoundsInTheOrderOfThePack()
    {
        // When
        var state = InPhase(GamePhase.RoundIntro);

        // Then
        Assert.Equal(
            [(0, ScheduledRoundStatus.Current), (1, ScheduledRoundStatus.Upcoming), (2, ScheduledRoundStatus.Upcoming)],
            StatusesOf(state));
        Assert.Equal((1, 3), NumberOf(state));
    }

    [Theory]
    [MemberData(nameof(ReorderablePhases))]
    public void Handle_ReorderRounds_NextRoundFollowsTheNewOrder(GamePhase phase)
    {
        // Given
        var state = InPhase(phase);

        // When
        state = Games.Accepted(state, Reorder(state, (2, false), (1, false)));

        // Then
        Assert.Equal(
            [(0, ScheduledRoundStatus.Current), (2, ScheduledRoundStatus.Upcoming), (1, ScheduledRoundStatus.Upcoming)],
            StatusesOf(state));
        state = Finished(state);
        state = Games.Accepted(state, Games.NextRound(state), seed: 43);
        Assert.Equal(2, state.CurrentRound!.Index);
        Assert.Equal((2, 3), NumberOf(state));
    }

    [Fact]
    public void Handle_ReorderRoundsBetweenRounds_ShowsTheNewNextRoundToTheGameMaster()
    {
        // Given
        var state = InPhase(GamePhase.BetweenRounds);

        // When
        state = Games.Accepted(state, Reorder(state, (2, false), (1, false)));

        // Then
        Assert.Equal("Finale", Games.Snapshots.ForGameMaster(state).NextRoundTitle);
    }

    [Fact]
    public void Handle_WithdrawARound_IsNeitherPlayedNorCounted()
    {
        // Given
        var state = InPhase(GamePhase.Round);

        // When
        state = Games.Accepted(state, Reorder(state, (2, false), (1, true)));

        // Then
        Assert.Equal((1, 2), NumberOf(state));
        state = Finished(state);
        state = Games.Accepted(state, Games.NextRound(state), seed: 43);
        Assert.Equal(2, state.CurrentRound!.Index);
        Assert.Equal((2, 2), NumberOf(state));
        Assert.Equal(
            [(0, ScheduledRoundStatus.Played), (2, ScheduledRoundStatus.Current), (1, ScheduledRoundStatus.Withdrawn)],
            StatusesOf(state));
        Assert.Equal(GamePhase.Finished, Finished(state).Phase);
    }

    [Fact]
    public void Handle_PutBackAWithdrawnRound_CountsItAgain()
    {
        // Given
        var state = InPhase(GamePhase.Round);
        state = Games.Accepted(state, Reorder(state, (2, false), (1, true)));

        // When
        state = Games.Accepted(state, Reorder(state, (2, false), (1, false)));

        // Then
        Assert.Equal((1, 3), NumberOf(state));
        Assert.Equal([2, 1], state.Schedule.Upcoming);
        Assert.Empty(state.Schedule.Withdrawn);
    }

    [Fact]
    public void Handle_EveryRoundToComeWithdrawn_TheRoundInProgressEndsTheGame()
    {
        // Given
        var state = InPhase(GamePhase.Round);
        state = Games.Accepted(state, Reorder(state, (1, true), (2, true)));

        // When
        state = Finished(state);

        // Then
        Assert.Equal(GamePhase.Finished, state.Phase);
        Assert.Equal(Games.Now, state.FinishedAt);
        Assert.Equal((1, 1), NumberOf(state));
    }

    [Fact]
    public void Handle_NextRoundOnceEveryRoundToComeIsWithdrawn_FinishesTheGame()
    {
        // Given
        var state = InPhase(GamePhase.BetweenRounds);
        state = Games.Accepted(state, Reorder(state, (1, true), (2, true)));

        // When
        state = Games.Accepted(state, Games.NextRound(state));

        // Then
        Assert.Equal(GamePhase.Finished, state.Phase);
        Assert.Equal(Games.Now, state.FinishedAt);
        Assert.Null(Games.Snapshots.ForGameMaster(state).NextRoundTitle);
    }

    [Fact]
    public void Handle_ReorderRoundsWhilePaused_ChangesTheProgramme()
    {
        // Given
        var state = InPhase(GamePhase.Round) with { PausedAt = Games.Now };

        // When
        var transition = Games.Engine.Handle(state, Reorder(state, (2, false), (1, false)), Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        Assert.Equal([2, 1], transition.State.Schedule.Upcoming);
    }

    [Fact]
    public void Handle_ReorderRoundsToTheSameOrder_IsAcceptedWithoutChange()
    {
        // Given
        var state = InPhase(GamePhase.Round);

        // When
        var transition = Games.Engine.Handle(state, Reorder(state, (1, false), (2, false)), Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        Assert.Same(state, transition.State);
    }

    [Fact]
    public void Handle_SkipRound_ListsItAsSkippedOnceTheNextRoundIsAnnounced()
    {
        // Given
        var state = InPhase(GamePhase.Round);
        state = Games.Accepted(state, Games.SkipRound(state));

        // When
        state = Games.Accepted(state, Games.NextRound(state), seed: 43);

        // Then
        Assert.Equal(
            [(0, ScheduledRoundStatus.Skipped), (1, ScheduledRoundStatus.Current), (2, ScheduledRoundStatus.Upcoming)],
            StatusesOf(state));
        Assert.Equal((2, 3), NumberOf(state));
    }

    [Theory]
    [InlineData(GamePhase.Lobby, RejectionReason.NotReorderable)]
    [InlineData(GamePhase.Finished, RejectionReason.NotReorderable)]
    [InlineData(GamePhase.ResumePending, RejectionReason.GamePending)]
    public void Handle_ReorderRoundsOutOfAGameInProgress_IsRejected(GamePhase phase, RejectionReason reason)
    {
        // Given
        var state = Games.InPhase(phase, "Zoé");

        // When
        var transition = Games.Engine.Handle(state, new ReorderRounds(state.GameId, [], [], Games.Now), Games.Context());

        // Then
        Assert.Equal(reason, transition.Rejection);
        Assert.Same(state, transition.State);
    }

    [Fact]
    public void Handle_ReorderRoundsOfAnotherGame_IsRejected()
    {
        // Given
        var state = InPhase(GamePhase.Round);
        var request = Reorder(state, (2, false), (1, false)) with { GameId = new GameId(Guid.Parse("11111111-2222-3333-4444-555555555555")) };

        // When
        var transition = Games.Engine.Handle(state, request, Games.Context());

        // Then
        Assert.Equal(RejectionReason.GameMismatch, transition.Rejection);
        Assert.Same(state, transition.State);
    }

    [Fact]
    public void Handle_ReorderRoundsTwice_SecondIsRejectedAsObsolete()
    {
        // Given: a second console that saw the programme before the first change
        var state = InPhase(GamePhase.Round);
        var second = Reorder(state, (1, true), (2, false));
        state = Games.Accepted(state, Reorder(state, (2, false), (1, false)));

        // When
        var transition = Games.Engine.Handle(state, second, Games.Context());

        // Then
        Assert.Equal(RejectionReason.ScheduleObsolete, transition.Rejection);
        Assert.Equal([2, 1], transition.State.Schedule.Upcoming);
    }

    [Theory]
    [InlineData(3, RejectionReason.RoundUnknown)]
    [InlineData(-1, RejectionReason.RoundUnknown)]
    [InlineData(0, RejectionReason.RoundFixed)]
    public void Handle_ReorderRoundsNamingARoundNotToCome_IsRejected(int index, RejectionReason reason)
    {
        // Given: round 0 is the one in progress
        var state = InPhase(GamePhase.Round);

        // When
        var transition = Games.Engine.Handle(state, Reorder(state, (index, false), (2, false), (1, false)), Games.Context());

        // Then
        Assert.Equal(reason, transition.Rejection);
        Assert.Same(state, transition.State);
    }

    [Fact]
    public void Handle_ReorderRoundsNamingARoundPlayed_IsRejected()
    {
        // Given: round 0 played, round 1 in progress
        var state = Games.NextRoundStarted(InPhase(GamePhase.BetweenRounds));

        // When
        var transition = Games.Engine.Handle(state, Reorder(state, (0, false), (2, false)), Games.Context());

        // Then
        Assert.Equal(RejectionReason.RoundFixed, transition.Rejection);
        Assert.Same(state, transition.State);
    }

    [Theory]
    [InlineData(new[] { 1 })]
    [InlineData(new[] { 1, 1 })]
    [InlineData(new[] { 2, 1, 2 })]
    public void Handle_ReorderRoundsNotHoldingEachRoundOnce_IsRejected(int[] order)
    {
        // Given
        var state = InPhase(GamePhase.Round);

        // When
        var transition = Games.Engine.Handle(state, Reorder(state, [.. order.Select(i => (i, false))]), Games.Context());

        // Then
        Assert.Equal(RejectionReason.ScheduleIncomplete, transition.Rejection);
        Assert.Same(state, transition.State);
    }

    [Fact]
    public void Handle_ReturnToLobbyAfterAReorder_ForgetsTheProgramme()
    {
        // Given
        var state = InPhase(GamePhase.Round);
        state = Games.Accepted(state, Reorder(state, (2, false), (1, true)));

        // When
        state = Games.Accepted(state, Games.ReturnToLobby(state));

        // Then
        Assert.Same(RoundSchedule.Empty, state.Schedule);
        Assert.Empty(Games.Snapshots.ForGameMaster(state).Schedule);
    }

    [Fact]
    public void Serialize_ReorderedProgramme_IsPersisted()
    {
        // Given
        var state = InPhase(GamePhase.BetweenRounds);
        state = Games.Accepted(state, Reorder(state, (2, false), (1, true)));

        // When
        var restored = JsonSerializer.Deserialize<GameState>(JsonSerializer.Serialize(state, _options), _options);

        // Then
        Assert.Equal([2], restored!.Schedule.Upcoming);
        Assert.Equal([1], restored.Schedule.Withdrawn);
    }

    /// <summary>A lobby where a pack of three rounds, all played by <see cref="FakeMode"/>, is chosen.</summary>
    internal static GameState ThreeRoundLobby()
    {
        var pack = Games.ValidPack(
            "programme",
            "Soirée en trois",
            [new FakeRoundDescriptor { Title = "Échauffement" }, new FakeRoundDescriptor { Title = "Intermède" }, new FakeRoundDescriptor { Title = "Finale" }]);
        return Games.Accepted(Games.Accepted(Games.LobbyWith("Zoé", "Max"), Games.Loaded(Games.Pack, pack)), Games.Select(pack.Id));
    }

    /// <summary>The request of a console that sees the programme of <paramref name="state"/>.</summary>
    internal static ReorderRounds Reorder(GameState state, params (int Index, bool Withdrawn)[] order) =>
        new(
            state.GameId,
            [.. state.Schedule.Upcoming.Select(i => new ScheduledRound(i, false)), .. state.Schedule.Withdrawn.Select(i => new ScheduledRound(i, true))],
            [.. order.Select(r => new ScheduledRound(r.Index, r.Withdrawn))],
            Games.Now);

    /// <summary>The game of a pack of three rounds, its first round announced, in progress, or just finished.</summary>
    private static GameState InPhase(GamePhase phase) => Games.PlayedUpTo(phase, ThreeRoundLobby());

    /// <summary>The game once the mode ended the round in progress, started first if only announced.</summary>
    private static GameState Finished(GameState state)
    {
        if (state.Phase == GamePhase.BetweenRounds)
        {
            return state;
        }

        if (state.Phase == GamePhase.RoundIntro)
        {
            state = Games.Accepted(state, Games.StartRound(state));
        }

        return Games.Accepted(state, Games.GameMasterActs(state, FakeGameMasterIntent.Finish));
    }

    private static ImmutableArray<(int, ScheduledRoundStatus)> StatusesOf(GameState state) =>
        [.. Games.Snapshots.ForGameMaster(state).Schedule.Select(r => (r.RoundIndex, r.Status))];

    private static (int Number, int Count) NumberOf(GameState state) =>
        Snapshots.RoundInfoOf(state) is { } round ? (round.Number, round.Count) : default;
}
