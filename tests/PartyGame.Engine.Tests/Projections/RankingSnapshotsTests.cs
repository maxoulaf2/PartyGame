using PartyGame.Contracts;
using PartyGame.Engine.Inputs;
using PartyGame.Engine.Tests.Rounds;

namespace PartyGame.Engine.Tests.Projections;

/// <summary>
/// The ranking each role sees between two rounds and once the game is finished: every player for the TV screen and the
/// game master, their own rank alone for a phone.
/// </summary>
public sealed class RankingSnapshotsTests
{
    public static TheoryData<GamePhase> Ranked => [GamePhase.BetweenRounds, GamePhase.Finished];

    [Theory]
    [MemberData(nameof(Ranked))]
    public void ForDisplayAndGameMaster_Ranked_ShowEveryPlayerByRank(GamePhase phase)
    {
        // Given
        var state = Games.WithScores(Games.InPhase(phase, "Zoé", "Max", "Léa"), 1000, 2000, 1000);
        state = Games.Accepted(state, new PlayerConnectionLost(Games.PlayerIdOf(3)));
        // Nobody scored in the first round: the last ranking finds everybody ex aequo before it.
        int? previous = phase == GamePhase.Finished ? 1 : null;
        RankedPlayer[] expected =
        [
            new(Games.PlayerIdOf(2), "Max", IsConnected: true, Rank: 1, IsTied: false, Score: 2000, previous),
            new(Games.PlayerIdOf(3), "Léa", IsConnected: false, Rank: 2, IsTied: true, Score: 1000, previous),
            new(Games.PlayerIdOf(1), "Zoé", IsConnected: true, Rank: 2, IsTied: true, Score: 1000, previous),
        ];

        // When
        var display = Games.Snapshots.ForDisplay(state);
        var gameMaster = Games.Snapshots.ForGameMaster(state);

        // Then
        Assert.Equal(expected, display.Ranking);
        Assert.Equal(expected, gameMaster.Ranking);
    }

    [Theory]
    [MemberData(nameof(Ranked))]
    public void ForPlayer_Ranked_ShowsTheRankOfThisPlayerOnly(GamePhase phase)
    {
        // Given
        var state = Games.WithScores(Games.InPhase(phase, "Zoé", "Max", "Léa"), 1000, 2000, 1000);

        // When
        var standings = state.Players.Select(p => Games.Snapshots.ForPlayer(state, p).Standing);

        // Then
        Assert.Equal(
            [new PlayerStanding(2, IsTied: true, RankedCount: 3), new PlayerStanding(1, IsTied: false, RankedCount: 3), new PlayerStanding(2, IsTied: true, RankedCount: 3)],
            standings);
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
        Assert.Equal((new PlayerStanding(2, IsTied: false, RankedCount: 2), 2), (player.Standing, player.PlayerCount));
    }

    [Fact]
    public void ForEachRole_PlayerWhoJoinedDuringTheGame_IsInTheFinalRanking()
    {
        // Given: Max joined between the two rounds, and scored in the last one
        var state = Games.InPhase(GamePhase.BetweenRounds, "Zoé");
        state = Games.Accepted(state, Games.Join("Max", player: 2));
        state = Games.NextRoundStarted(state);
        state = Games.Accepted(Games.WithScores(state, 1000, 0), Games.GameMasterActs(state, FakeGameMasterIntent.Award));
        state = Games.Accepted(state, Games.GameMasterActs(state, FakeGameMasterIntent.Finish));

        // When
        var display = Games.Snapshots.ForDisplay(state);
        var player = Games.Snapshots.ForPlayer(state, state.Players[1]);

        // Then
        Assert.Equal(Phase.Finished, display.Phase);
        Assert.Equal([("Zoé", 1, 1010), ("Max", 2, 10)], display.Ranking.Select(p => (p.Nickname, p.Rank, p.Score)));
        Assert.Equal(new PlayerStanding(2, IsTied: false, RankedCount: 2), player.Standing);
    }

    [Fact]
    public void ForEachRole_PlayerWhoJoinedOnceFinished_IsNotRanked()
    {
        // Given
        var state = Games.WithScores(Games.InPhase(GamePhase.Finished, "Zoé", "Max"), 1000, 0);
        state = Games.Accepted(state, Games.Join("Léa", player: 3));

        // When
        var display = Games.Snapshots.ForDisplay(state);
        var gameMaster = Games.Snapshots.ForGameMaster(state);
        var players = state.Players.Select(p => Games.Snapshots.ForPlayer(state, p)).ToArray();

        // Then: the final ranking stays that of the game, Léa seeing its end without a rank
        Assert.Equal(["Zoé", "Max"], display.Ranking.Select(p => p.Nickname));
        Assert.Equal(display.Ranking, gameMaster.Ranking);
        Assert.Equal(
            [new PlayerStanding(1, IsTied: false, RankedCount: 2), new PlayerStanding(2, IsTied: false, RankedCount: 2), null],
            players.Select(p => p.Standing));
        Assert.Equal(Phase.Finished, players[2].Phase);
        Assert.Contains(gameMaster.Players, p => p.Nickname == "Léa");
    }

    [Theory]
    [InlineData(GamePhase.Lobby)]
    [InlineData(GamePhase.Round)]
    public void ForEachRole_NeitherBetweenRoundsNorFinished_ShowNoRanking(GamePhase phase)
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

    [Fact]
    public void ForDisplayAndGameMaster_FirstRanking_ShowNoPreviousRank()
    {
        // Given
        var state = Games.WithScores(Games.InPhase(GamePhase.BetweenRounds, "Zoé", "Max"), 1000, 2000);

        // When
        var display = Games.Snapshots.ForDisplay(state);
        var gameMaster = Games.Snapshots.ForGameMaster(state);

        // Then
        Assert.All(display.Ranking, p => Assert.Null(p.PreviousRank));
        Assert.Equal(display.Ranking, gameMaster.Ranking);
    }

    [Fact]
    public void ForDisplayAndGameMaster_LaterRanking_ShowTheRankOfEachPlayerInTheRankingBefore()
    {
        // Given: Max leads after the first round, ahead of Zoé and Léa ex aequo, then Zoé takes the lead
        var state = Games.WithScores(Games.InPhase(GamePhase.BetweenRounds, "Zoé", "Max", "Léa"), 1000, 2000, 1000);
        state = Games.NextRoundStarted(state);
        state = Games.WithScores(state, 3000, 2000, 1000);

        // When
        state = Games.Accepted(state, Games.GameMasterActs(state, FakeGameMasterIntent.Finish));
        var display = Games.Snapshots.ForDisplay(state);
        var gameMaster = Games.Snapshots.ForGameMaster(state);

        // Then
        Assert.Equal(
            [("Zoé", 1, (int?)2), ("Max", 2, 1), ("Léa", 3, 2)],
            display.Ranking.Select(p => (p.Nickname, p.Rank, p.PreviousRank)));
        Assert.Equal(display.Ranking, gameMaster.Ranking);
    }

    [Fact]
    public void ForDisplay_PlayerWhoJoinedSinceThePreviousRanking_HasNoPreviousRank()
    {
        // Given: Max joins during the introduction of the second round
        var state = Games.WithScores(Games.InPhase(GamePhase.BetweenRounds, "Zoé"), 1000);
        state = Games.Accepted(state, Games.NextRound(state), seed: 43);
        state = Games.Accepted(state, Games.Join("Max", player: 2));
        state = Games.Accepted(state, Games.StartRound(state));

        // When
        state = Games.Accepted(state, Games.GameMasterActs(state, FakeGameMasterIntent.Finish));
        var display = Games.Snapshots.ForDisplay(state);

        // Then
        Assert.Equal([("Zoé", (int?)1), ("Max", null)], display.Ranking.Select(p => (p.Nickname, p.PreviousRank)));
    }

    [Fact]
    public void ForDisplayAndPlayer_Finished_TellWhenTheGameFinished()
    {
        // Given
        var state = Games.InPhase(GamePhase.Finished, "Zoé", "Max");

        // When
        var display = Games.Snapshots.ForDisplay(state);
        var player = Games.Snapshots.ForPlayer(state, state.Players[0]);

        // Then
        Assert.Equal(Games.Now.ToUnixTimeMilliseconds(), display.FinishedAt);
        Assert.Equal(display.FinishedAt, player.FinishedAt);
    }

    [Fact]
    public void ForDisplayAndPlayer_LastRoundSkipped_TellWhenTheGameFinished()
    {
        // Given
        var state = Games.InLastRound("Zoé", "Max");

        // When
        state = Games.Accepted(state, Games.SkipRound(state));

        // Then
        Assert.Equal(Phase.Finished, Games.Snapshots.ForDisplay(state).Phase);
        Assert.Equal(Games.Now.ToUnixTimeMilliseconds(), Games.Snapshots.ForDisplay(state).FinishedAt);
    }

    [Fact]
    public void ForDisplayAndPlayer_BetweenRounds_TellNoEnd()
    {
        // Given
        var state = Games.InPhase(GamePhase.BetweenRounds, "Zoé", "Max");

        // When
        var display = Games.Snapshots.ForDisplay(state);
        var player = Games.Snapshots.ForPlayer(state, state.Players[0]);

        // Then
        Assert.Null(display.FinishedAt);
        Assert.Null(player.FinishedAt);
    }
}
