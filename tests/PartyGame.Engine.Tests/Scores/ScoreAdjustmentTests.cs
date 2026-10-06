using System.Text.Json;
using PartyGame.Engine.Inputs;
using PartyGame.Engine.Tests.Rounds;

namespace PartyGame.Engine.Tests.Scores;

/// <summary>
/// Correction of a score by the game master, at any moment of a started game: the request names the score it corrects.
/// </summary>
public sealed class ScoreAdjustmentTests
{
    private static readonly JsonSerializerOptions _options = GameStateJson.CreateOptions(Games.Modes, FakeJson.Options);

    public static TheoryData<GamePhase> StartedPhases =>
        [GamePhase.RoundIntro, GamePhase.Round, GamePhase.BetweenRounds, GamePhase.Finished];

    [Theory]
    [MemberData(nameof(StartedPhases))]
    public void Handle_AdjustScoreOnceStarted_ChangesTheScoreOfThePlayerOnly(GamePhase phase)
    {
        // Given
        var state = Games.WithScores(Games.InPhase(phase, "Zoé", "Max"), 300, 100);

        // When
        var transition = Games.Engine.Handle(state, Adjust(player: 2, expected: 100, now: 600), Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        Assert.Empty(transition.Effects);
        Assert.Equal([300, 600], transition.State.Players.Select(p => p.Score));
    }

    [Fact]
    public void Handle_AdjustScoreWhilePaused_ChangesTheScore()
    {
        // Given
        var state = Games.WithScores(Games.InPhase(GamePhase.Round, "Zoé"), 300) with { PausedAt = Games.Now };

        // When
        var transition = Games.Engine.Handle(state, Adjust(player: 1, expected: 300, now: 0), Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        Assert.Equal(0, transition.State.Players[0].Score);
    }

    [Fact]
    public void Handle_AdjustScoreDuringAQuestion_PointsOfTheRevealAddToTheAdjustedScore()
    {
        // Given
        var state = Games.InPhase(GamePhase.Round, "Zoé");
        state = Games.Accepted(state, Adjust(player: 1, expected: 0, now: 500));

        // When
        state = Games.Accepted(state, Games.GameMasterActs(state, FakeGameMasterIntent.Award));

        // Then
        Assert.Equal(500 + FakeMode.AwardedPoints, state.Players[0].Score);
    }

    [Fact]
    public void Handle_AdjustScoreToTheSameScore_IsAcceptedWithoutChange()
    {
        // Given
        var state = Games.WithScores(Games.InPhase(GamePhase.Round, "Zoé"), 300);

        // When
        var transition = Games.Engine.Handle(state, Adjust(player: 1, expected: 300, now: 300), Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        Assert.Same(state, transition.State);
    }

    [Theory]
    [InlineData(GamePhase.Lobby, RejectionReason.NotAdjustable)]
    [InlineData(GamePhase.ResumePending, RejectionReason.GamePending)]
    public void Handle_AdjustScoreOutOfAGame_IsRejected(GamePhase phase, RejectionReason reason)
    {
        // Given
        var state = Games.InPhase(phase, "Zoé");

        // When
        var transition = Games.Engine.Handle(state, Adjust(player: 1, expected: 0, now: 500), Games.Context());

        // Then
        Assert.Equal(reason, transition.Rejection);
        Assert.Same(state, transition.State);
    }

    [Fact]
    public void Handle_AdjustScoreOfAnUnknownPlayer_IsRejected()
    {
        // Given
        var state = Games.InPhase(GamePhase.Round, "Zoé");

        // When
        var transition = Games.Engine.Handle(state, Adjust(player: 9, expected: 0, now: 500), Games.Context());

        // Then
        Assert.Equal(RejectionReason.PlayerUnknown, transition.Rejection);
        Assert.Same(state, transition.State);
    }

    [Fact]
    public void Handle_AdjustScoreBelowZero_IsRejected()
    {
        // Given
        var state = Games.WithScores(Games.InPhase(GamePhase.Round, "Zoé"), 300);

        // When
        var transition = Games.Engine.Handle(state, Adjust(player: 1, expected: 300, now: -200), Games.Context());

        // Then
        Assert.Equal(RejectionReason.ScoreNegative, transition.Rejection);
        Assert.Same(state, transition.State);
    }

    [Fact]
    public void Handle_AdjustScoreTwice_SecondIsRejectedAsObsolete()
    {
        // Given: a second console that saw the score before the first adjustment
        var state = Games.WithScores(Games.InPhase(GamePhase.Round, "Zoé"), 300);
        state = Games.Accepted(state, Adjust(player: 1, expected: 300, now: 800));

        // When
        var transition = Games.Engine.Handle(state, Adjust(player: 1, expected: 300, now: 100), Games.Context());

        // Then
        Assert.Equal(RejectionReason.ScoreObsolete, transition.Rejection);
        Assert.Equal(800, transition.State.Players[0].Score);
    }

    [Fact]
    public void Handle_ReturnToLobbyAfterAnAdjustment_ForgetsIt()
    {
        // Given
        var state = Games.InPhase(GamePhase.BetweenRounds, "Zoé");
        state = Games.Accepted(state, Adjust(player: 1, expected: state.Players[0].Score, now: 900));

        // When
        state = Games.Accepted(state, Games.ReturnToLobby(state));

        // Then
        Assert.Equal(0, state.Players[0].Score);
    }

    [Fact]
    public void Serialize_AdjustedScore_IsPersisted()
    {
        // Given
        var state = Games.InPhase(GamePhase.Round, "Zoé");
        state = Games.Accepted(state, Adjust(player: 1, expected: 0, now: 700));

        // When
        var restored = JsonSerializer.Deserialize<GameState>(JsonSerializer.Serialize(state, _options), _options);

        // Then
        Assert.Equal(700, restored!.Players[0].Score);
    }

    private static AdjustScore Adjust(int player, int expected, int now) => new(Games.PlayerIdOf(player), expected, now, Games.Now);
}
