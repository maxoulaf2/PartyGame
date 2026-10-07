using PartyGame.Engine.Scores;
using PartyGame.Engine.State;

namespace PartyGame.Engine.Tests.Scores;

public sealed class RankingTests
{
    [Fact]
    public void Of_DistinctScores_RanksByDescendingScore()
    {
        // Given
        var players = Games.WithScores(Games.LobbyWith("Zoé", "Max", "Léa"), 1000, 3000, 2000).Players;

        // When
        var ranking = Ranking.Of(players);

        // Then
        Assert.Equal(
            [("Max", 1, false, 3000), ("Léa", 2, false, 2000), ("Zoé", 3, false, 1000)],
            ranking.Select(s => (s.Player.Nickname, s.Rank, s.IsTied, s.Player.Score)));
    }

    [Fact]
    public void Of_TiedScores_ShareTheirRankAndTheNextRankCountsThem()
    {
        // Given
        var players = Games.WithScores(Games.LobbyWith("Zoé", "Max", "Léa", "Bob"), 2000, 2000, 1000, 1000).Players;

        // When
        var ranking = Ranking.Of(players);

        // Then
        Assert.Equal(
            [("Max", 1, true), ("Zoé", 1, true), ("Bob", 3, true), ("Léa", 3, true)],
            ranking.Select(s => (s.Player.Nickname, s.Rank, s.IsTied)));
    }

    [Fact]
    public void Of_TieInTheMiddle_LeavesTheOthersUntied()
    {
        // Given
        var players = Games.WithScores(Games.LobbyWith("Zoé", "Max", "Léa", "Bob"), 3000, 2000, 2000, 1000).Players;

        // When
        var ranking = Ranking.Of(players);

        // Then
        Assert.Equal(
            [("Zoé", 1, false), ("Léa", 2, true), ("Max", 2, true), ("Bob", 4, false)],
            ranking.Select(s => (s.Player.Nickname, s.Rank, s.IsTied)));
    }

    [Fact]
    public void Of_TiedScores_AreInAlphabeticalOrderRegardlessOfAccentsAndCase()
    {
        // Given: by code point, « éric » and « bob » would come after « Zoé ».
        var players = Games.WithScores(Games.LobbyWith("Zoé", "éric", "bob", "Emma"), 0, 0, 0, 0).Players;

        // When
        var ranking = Ranking.Of(players);

        // Then
        Assert.Equal(["bob", "Emma", "éric", "Zoé"], ranking.Select(s => s.Player.Nickname));
        Assert.All(ranking, s => Assert.Equal((1, true), (s.Rank, s.IsTied)));
    }

    [Fact]
    public void Of_DisconnectedPlayerAndLateArrival_AreRankedLikeTheOthers()
    {
        // Given
        var state = Games.WithScores(Games.InPhase(GamePhase.Round, "Zoé", "Max"), 1000, 2000);
        state = Games.Accepted(state, Games.Join("Léa", player: 3));
        state = Games.Accepted(state, new Inputs.PlayerConnectionLost(Games.PlayerIdOf(2)));

        // When
        var ranking = Ranking.Of(state.Players);

        // Then
        Assert.Equal(
            [("Max", 1, false, 2000), ("Zoé", 2, true, 1000), ("Léa", 3, true, 0)],
            ranking.Select(s => (s.Player.Nickname, s.Rank, s.Player.IsConnected, s.Player.Score)));
    }

    [Fact]
    public void Of_NoPlayer_IsEmpty()
    {
        // When
        var ranking = Ranking.Of([]);

        // Then
        Assert.Empty(ranking);
    }
}
