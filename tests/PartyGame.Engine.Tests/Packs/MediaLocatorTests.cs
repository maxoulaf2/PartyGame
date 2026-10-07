using PartyGame.Contracts.Packs;
using PartyGame.Engine.Packs;
using PartyGame.Engine.Projections;
using PartyGame.Engine.State;

namespace PartyGame.Engine.Tests.Packs;

public sealed class MediaLocatorTests
{
    private readonly MediaLocator _locator = new(Games.Modes);

    [Fact]
    public void Locate_MediaOfTheRoundInProgress_GivesItsPathAndRound()
    {
        // Given
        var state = Games.PlayedUpTo(GamePhase.Round, Games.IllustratedLobbyWith("Zoé"));

        // When
        var location = _locator.Locate(state, IdOf(state, Games.Flag));

        // Then: the fake mode tells no step, as any mode that does not override it
        Assert.Equal(new MediaLocation(Games.Flag, Snapshots.RoundInfoOf(state), Step: null), location);
    }

    [Fact]
    public void Locate_MediaBetweenTwoRounds_GivesNoRound()
    {
        // Given
        var state = Games.PlayedUpTo(GamePhase.BetweenRounds, Games.IllustratedLobbyWith("Zoé"));

        // When
        var location = _locator.Locate(state, IdOf(state, Games.Monument));

        // Then: the last round played is over
        Assert.Equal(new MediaLocation(Games.Monument, Round: null, Step: null), location);
    }

    [Fact]
    public void Locate_UnknownIdentifier_GivesNothing()
    {
        // Given
        var state = Games.PlayedUpTo(GamePhase.Round, Games.IllustratedLobbyWith("Zoé"));

        // When
        var location = _locator.Locate(state, new MediaId("AAAAAAAAAAAAAAAAAAAAAA"));

        // Then
        Assert.Null(location);
    }

    [Fact]
    public void Locate_InTheLobby_GivesNothing()
    {
        // Given: the identifiers are drawn when the game starts
        var lobby = Games.IllustratedLobbyWith("Zoé");
        var started = Games.PlayedUpTo(GamePhase.Round, lobby);

        // When
        var location = _locator.Locate(lobby, IdOf(started, Games.Flag));

        // Then
        Assert.Null(location);
    }

    private static MediaId IdOf(GameState state, MediaPath media) => state.Media.Files.Single(file => file.Value == media).Key;
}
