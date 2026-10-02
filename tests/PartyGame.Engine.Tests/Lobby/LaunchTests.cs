using PartyGame.Engine.Inputs;

namespace PartyGame.Engine.Tests.Lobby;

public sealed class LaunchTests
{
    [Fact]
    public void Handle_StartGameWithPlayers_StartsTheGameAndKeepsThePlayers()
    {
        // Given
        var state = Games.LobbyWith("Zoé", "Max");

        // When
        var transition = Games.Engine.Handle(state, Games.Start(), Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        Assert.Empty(transition.Effects);
        Assert.Equal(state with { Phase = GamePhase.Started }, transition.State);
    }

    [Fact]
    public void Handle_StartGameWithOnlyDisconnectedPlayers_StartsTheGame()
    {
        // Given
        var state = Games.LobbyWith("Zoé");
        state = Games.Engine.Handle(state, new PlayerConnectionLost(Games.PlayerIdOf(1)), Games.Context()).State;

        // When
        var transition = Games.Engine.Handle(state, Games.Start(), Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        Assert.Equal(GamePhase.Started, transition.State.Phase);
    }

    [Fact]
    public void Handle_StartGameWithoutPlayers_IsRejected()
    {
        // Given
        var state = Games.NewLobby();

        // When
        var transition = Games.Engine.Handle(state, Games.Start(), Games.Context());

        // Then
        Assert.Same(state, transition.State);
        Assert.Empty(transition.Effects);
        Assert.Equal(RejectionReason.NotEnoughPlayers, transition.Rejection);
    }

    [Fact]
    public void Handle_StartGameTwice_RejectsTheSecond()
    {
        // Given
        var state = Games.Engine.Handle(Games.LobbyWith("Zoé"), Games.Start(), Games.Context()).State;

        // When
        var transition = Games.Engine.Handle(state, Games.Start(), Games.Context());

        // Then
        Assert.Same(state, transition.State);
        Assert.Empty(transition.Effects);
        Assert.Equal(RejectionReason.GameAlreadyStarted, transition.Rejection);
    }

    [Fact]
    public void Handle_JoinGameAfterStart_RegistersThePlayerAndStaysStarted()
    {
        // Given
        var state = Games.Engine.Handle(Games.LobbyWith("Zoé"), Games.Start(), Games.Context()).State;

        // When
        var transition = Games.Engine.Handle(state, Games.Join("Max", player: 2), Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        Assert.Equal(GamePhase.Started, transition.State.Phase);
        Assert.Equal(["Zoé", "Max"], transition.State.Players.Select(p => p.Nickname));
    }
}
