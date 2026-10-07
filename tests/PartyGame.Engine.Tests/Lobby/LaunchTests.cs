using PartyGame.Contracts.Packs;
using PartyGame.Engine.Inputs;
using PartyGame.Engine.State;
using PartyGame.Engine.Tests.Rounds;

namespace PartyGame.Engine.Tests.Lobby;

public sealed class LaunchTests
{
    [Fact]
    public void Handle_StartGameWithPlayers_AnnouncesTheFirstRoundAndKeepsThePlayers()
    {
        // Given
        var state = Games.InPhase(GamePhase.Lobby, "Zoé", "Max");

        // When
        var transition = Games.Engine.Handle(state, Games.Start(), Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        Assert.Equal(GamePhase.RoundIntro, transition.State.Phase);
        Assert.Equal(0, transition.State.CurrentRound!.Index);
        Assert.Equal(state.Players, transition.State.Players);
        Assert.Equal(state.PlayerTokens, transition.State.PlayerTokens);
    }

    [Fact]
    public void Handle_StartGame_CopiesTheSelectedPackIntoTheGame()
    {
        // Given
        var other = Games.ValidPack("autre", "Autre soirée", [Games.TwoRounds[1]]);
        var state = Games.Accepted(Games.Accepted(Games.LobbyWith("Zoé"), Games.Loaded(Games.Pack, other)), Games.Select(other.Id));

        // When
        var transition = Games.Engine.Handle(state, Games.Start(), Games.Context());

        // Then: the game no longer depends on the catalog
        Assert.Null(transition.Rejection);
        Assert.Same(other.Descriptor, transition.State.Pack);
        Assert.Equal(other.Descriptor!.Rounds, transition.State.Rounds);
        Assert.Equal(other.Id, transition.State.SelectedPackId);
    }

    [Fact]
    public void Handle_StartGameWithoutSelectedPack_IsRejected()
    {
        // Given: several valid packs, and none chosen yet
        var state = Games.Accepted(Games.LobbyWith("Zoé"), Games.Loaded());
        state = Games.Accepted(state, Games.Loaded(Games.Pack, Games.ValidPack("autre", "Autre soirée", Games.TwoRounds)));

        // When
        var transition = Games.Engine.Handle(state, Games.Start(), Games.Context());

        // Then
        Assert.Same(state, transition.State);
        Assert.Empty(transition.Effects);
        Assert.Equal(RejectionReason.PackNotSelected, transition.Rejection);
    }

    [Fact]
    public void Handle_StartGameWithoutAnyPack_IsRejected()
    {
        // Given
        var state = Games.Accepted(Games.LobbyWith("Zoé"), Games.Loaded());

        // When
        var transition = Games.Engine.Handle(state, Games.Start(), Games.Context());

        // Then
        Assert.Same(state, transition.State);
        Assert.Empty(transition.Effects);
        Assert.Equal(RejectionReason.PackNotSelected, transition.Rejection);
    }

    [Fact]
    public void Handle_StartGameWithOnlyDisconnectedPlayers_StartsTheGame()
    {
        // Given
        var state = Games.InPhase(GamePhase.Lobby, "Zoé");
        state = Games.Engine.Handle(state, new PlayerConnectionLost(Games.PlayerIdOf(1)), Games.Context()).State;

        // When
        var transition = Games.Engine.Handle(state, Games.Start(), Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        Assert.Equal(GamePhase.RoundIntro, transition.State.Phase);
    }

    [Fact]
    public void Handle_StartGameWithoutPlayers_IsRejected()
    {
        // Given
        var state = Games.InPhase(GamePhase.Lobby);

        // When
        var transition = Games.Engine.Handle(state, Games.Start(), Games.Context());

        // Then
        Assert.Same(state, transition.State);
        Assert.Empty(transition.Effects);
        Assert.Equal(RejectionReason.NotEnoughPlayers, transition.Rejection);
    }

    [Theory]
    [InlineData(GamePhase.Round)]
    [InlineData(GamePhase.BetweenRounds)]
    [InlineData(GamePhase.Finished)]
    public void Handle_StartGameOnceStarted_IsRejected(GamePhase phase)
    {
        // Given: a double tap, or a second game master
        var state = Games.InPhase(phase, "Zoé");

        // When
        var transition = Games.Engine.Handle(state, Games.Start(), Games.Context());

        // Then
        Assert.Same(state, transition.State);
        Assert.Empty(transition.Effects);
        Assert.Equal(RejectionReason.GameAlreadyStarted, transition.Rejection);
    }

    [Fact]
    public void Handle_StartGame_DrawsAnIdentifierForEachMediaOfTheSelectedPackOnly()
    {
        // Given: another pack, which is not played, has images too
        var other = Games.ValidPack(
            "autre",
            "Autre soirée",
            [new FakeRoundDescriptor { Title = "Ailleurs", Image = new MediaPath("images/autre.png") }]);
        var state = Games.IllustratedLobbyWith("Zoé");
        state = Games.Accepted(state, Games.Loaded(Games.IllustratedPack(), other));

        // When
        var transition = Games.Engine.Handle(state, Games.Start(), Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        Assert.Equal(
            [Games.Flag.Value, Games.Monument.Value],
            transition.State.Media.Files.Values.Select(m => m.Value).Order(StringComparer.Ordinal));
    }

    [Fact]
    public void Handle_StartGameRejected_DrawsNoIdentifier()
    {
        // Given
        var state = Games.Accepted(Games.Accepted(Games.NewLobby(), Games.Loaded(Games.IllustratedPack())), Games.Select("illustre"));

        // When: no player yet
        var transition = Games.Engine.Handle(state, Games.Start(), Games.Context());

        // Then
        Assert.Equal(RejectionReason.NotEnoughPlayers, transition.Rejection);
        Assert.Empty(transition.State.Media.Files);
    }

    [Fact]
    public void Handle_NextRound_KeepsTheIdentifiersOfTheMedia()
    {
        // Given
        var state = Games.PlayedUpTo(GamePhase.BetweenRounds, Games.IllustratedLobbyWith("Zoé"));

        // When
        var next = Games.NextRoundStarted(state);

        // Then: a URL serves the same file for the whole game
        Assert.Same(state.Media, next.Media);
    }
}
