using PartyGame.Contracts;

namespace PartyGame.Engine.Tests.Projections;

/// <summary>
/// The snapshots of a restarted server that waits for the game master to resume the game found or start a new one.
/// </summary>
public sealed class PendingSnapshotsTests
{
    [Fact]
    public void ForGameMaster_GameFoundInARound_DescribesWhereItStoppedAndWithHowManyPlayers()
    {
        // Given
        var found = Games.InPhase(GamePhase.Round, "Zoé", "Max", "Léa");
        var state = Games.Pending(found, Games.Flag);

        // When
        var snapshot = Games.Snapshots.ForGameMaster(state);

        // Then
        Assert.Equal(Phase.ResumePending, snapshot.Phase);
        var savedGame = snapshot.SavedGame!;
        Assert.Equal([Games.Flag.Value], savedGame.MissingMedia);
        Assert.Equal(
            new GameMasterSavedGame(
                found.GameId,
                Games.SavedAt.ToUnixTimeMilliseconds(),
                Phase.Round,
                Games.Pack.Title,
                new RoundInfo(found.CurrentRound!.Id, 1, 2, "Échauffement"),
                new RoundStep(1, 10),
                PlayerCount: 3,
                savedGame.MissingMedia),
            savedGame);
    }

    [Theory]
    [InlineData(GamePhase.Lobby, Phase.Lobby)]
    [InlineData(GamePhase.BetweenRounds, Phase.BetweenRounds)]
    [InlineData(GamePhase.Finished, Phase.Finished)]
    public void ForGameMaster_GameFoundOutsideARound_DescribesItWithoutStep(GamePhase phase, Phase expected)
    {
        // Given
        var state = Games.Pending(Games.InPhase(phase, "Zoé"));

        // When
        var savedGame = Games.Snapshots.ForGameMaster(state).SavedGame!;

        // Then
        Assert.Equal(expected, savedGame.Phase);
        Assert.Null(savedGame.Step);
        Assert.Empty(savedGame.MissingMedia);
    }

    [Fact]
    public void ForGameMaster_NoGameFound_DescribesNone()
    {
        // Given
        var state = Games.InPhase(GamePhase.Round, "Zoé");

        // When
        var snapshot = Games.Snapshots.ForGameMaster(state);

        // Then
        Assert.Null(snapshot.SavedGame);
    }

    [Fact]
    public void ForDisplay_GameFound_ShowsThePendingPhaseWithNeitherPlayerNorPack()
    {
        // Given
        var state = Games.Pending(Games.InPhase(GamePhase.BetweenRounds, "Zoé", "Max"));

        // When
        var snapshot = Games.Snapshots.ForDisplay(state);

        // Then
        Assert.Equal(Phase.ResumePending, snapshot.Phase);
        Assert.Empty(snapshot.Players);
        Assert.Null(snapshot.PackTitle);
        Assert.Null(snapshot.Round);
        Assert.Empty(snapshot.Ranking);
    }
}
