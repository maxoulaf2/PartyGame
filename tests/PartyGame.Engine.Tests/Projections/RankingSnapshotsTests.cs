using PartyGame.Contracts;
using PartyGame.Engine.Inputs;

namespace PartyGame.Engine.Tests.Projections;

/// <summary>
/// The ranking each role sees between two rounds: every player for the TV screen and the game master, their own rank
/// alone for a phone.
/// </summary>
public sealed class RankingSnapshotsTests
{
    [Fact]
    public void ForDisplayAndGameMaster_BetweenRounds_ShowEveryPlayerByRank()
    {
        // Given
        var state = Games.WithScores(Games.InPhase(GamePhase.BetweenRounds, "Zoé", "Max", "Léa"), 1000, 2000, 1000);
        state = Games.Accepted(state, new PlayerConnectionLost(Games.PlayerIdOf(3)));
        RankedPlayer[] expected =
        [
            new(Games.PlayerIdOf(2), "Max", IsConnected: true, Rank: 1, IsTied: false, Score: 2000),
            new(Games.PlayerIdOf(3), "Léa", IsConnected: false, Rank: 2, IsTied: true, Score: 1000),
            new(Games.PlayerIdOf(1), "Zoé", IsConnected: true, Rank: 2, IsTied: true, Score: 1000),
        ];

        // When
        var display = Games.Snapshots.ForDisplay(state);
        var gameMaster = Games.Snapshots.ForGameMaster(state);

        // Then
        Assert.Equal(expected, display.Ranking);
        Assert.Equal(expected, gameMaster.Ranking);
    }

    [Fact]
    public void ForPlayer_BetweenRounds_ShowsTheRankOfThisPlayerOnly()
    {
        // Given
        var state = Games.WithScores(Games.InPhase(GamePhase.BetweenRounds, "Zoé", "Max", "Léa"), 1000, 2000, 1000);

        // When
        var standings = state.Players.Select(p => Games.Snapshots.ForPlayer(state, p).Standing);

        // Then
        Assert.Equal([new PlayerStanding(2, IsTied: true), new PlayerStanding(1, IsTied: false), new PlayerStanding(2, IsTied: true)], standings);
    }

    [Fact]
    public void ForEachRole_PlayerWhoJoinedBetweenRounds_IsRankedWithNoPoint()
    {
        // Given
        var state = Games.WithScores(Games.InPhase(GamePhase.BetweenRounds, "Zoé"), 1000);
        state = Games.Accepted(state, Games.Join("Max", player: 2));

        // When
        var display = Games.Snapshots.ForDisplay(state);
        var gameMaster = Games.Snapshots.ForGameMaster(state);
        var player = Games.Snapshots.ForPlayer(state, state.Players[1]);

        // Then
        Assert.Equal([("Zoé", 1, 1000), ("Max", 2, 0)], display.Ranking.Select(p => (p.Nickname, p.Rank, p.Score)));
        Assert.Equal(display.Ranking, gameMaster.Ranking);
        Assert.Equal((new PlayerStanding(2, IsTied: false), 2), (player.Standing, player.PlayerCount));
    }

    [Theory]
    [InlineData(GamePhase.Lobby)]
    [InlineData(GamePhase.Round)]
    [InlineData(GamePhase.Finished)]
    public void ForEachRole_NotBetweenRounds_ShowNoRanking(GamePhase phase)
    {
        // Given
        var state = Games.WithScores(Games.InPhase(phase, "Zoé", "Max"), 1000, 2000);

        // When
        var display = Games.Snapshots.ForDisplay(state);
        var gameMaster = Games.Snapshots.ForGameMaster(state);
        var player = Games.Snapshots.ForPlayer(state, state.Players[0]);

        // Then
        Assert.Empty(display.Ranking);
        Assert.Empty(gameMaster.Ranking);
        Assert.Null(player.Standing);
    }

    [Fact]
    public void ForGameMaster_BetweenRounds_ShowsTheTitleOfTheNextRound()
    {
        // Given
        var state = Games.InPhase(GamePhase.BetweenRounds, "Zoé");

        // When
        var snapshot = Games.Snapshots.ForGameMaster(state);

        // Then
        Assert.Equal("Finale", snapshot.NextRoundTitle);
    }

    [Theory]
    [InlineData(GamePhase.Lobby)]
    [InlineData(GamePhase.Round)]
    [InlineData(GamePhase.Finished)]
    public void ForGameMaster_NotBetweenRounds_ShowsNoNextRound(GamePhase phase)
    {
        // Given
        var state = Games.InPhase(phase, "Zoé");

        // When
        var snapshot = Games.Snapshots.ForGameMaster(state);

        // Then
        Assert.Null(snapshot.NextRoundTitle);
    }
}
