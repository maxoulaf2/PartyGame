using PartyGame.Engine.Inputs;

namespace PartyGame.Engine.Tests.Lobby;

public sealed class PresenceTests
{
    [Fact]
    public void Handle_PlayerConnectionLost_MarksOnlyThatPlayerDisconnected()
    {
        // Given
        var state = Games.LobbyWith("Zoé", "Max");

        // When
        var transition = Games.Engine.Handle(state, new PlayerConnectionLost(Games.PlayerIdOf(2)), Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        Assert.Empty(transition.Effects);
        Assert.Equal(
            [new Player(Games.PlayerIdOf(1), "Zoé", IsConnected: true), new Player(Games.PlayerIdOf(2), "Max", IsConnected: false)],
            transition.State.Players);
        Assert.Equal(state.PlayerTokens, transition.State.PlayerTokens);
    }

    [Fact]
    public void Handle_PlayerConnectionLostTwice_IsRejectedAsAlreadyDisconnected()
    {
        // Given
        var lost = new PlayerConnectionLost(Games.PlayerIdOf(1));
        var state = Games.Engine.Handle(Games.LobbyWith("Zoé"), lost, Games.Context()).State;

        // When
        var transition = Games.Engine.Handle(state, lost, Games.Context());

        // Then
        Assert.Same(state, transition.State);
        Assert.Empty(transition.Effects);
        Assert.Equal(RejectionReason.PlayerAlreadyDisconnected, transition.Rejection);
    }

    [Fact]
    public void Handle_PlayerConnectionLostOfUnknownPlayer_IsRejected()
    {
        // Given
        var state = Games.LobbyWith("Zoé");

        // When
        var transition = Games.Engine.Handle(state, new PlayerConnectionLost(Games.PlayerIdOf(9)), Games.Context());

        // Then
        Assert.Same(state, transition.State);
        Assert.Empty(transition.Effects);
        Assert.Equal(RejectionReason.PlayerUnknown, transition.Rejection);
    }

    [Fact]
    public void Handle_PlayerConnectionRestored_MarksOnlyThatPlayerConnectedAgain()
    {
        // Given
        var state = Games.LobbyWith("Zoé", "Max");
        state = Games.Engine.Handle(state, new PlayerConnectionLost(Games.PlayerIdOf(1)), Games.Context()).State;
        state = Games.Engine.Handle(state, new PlayerConnectionLost(Games.PlayerIdOf(2)), Games.Context()).State;

        // When
        var transition = Games.Engine.Handle(state, new PlayerConnectionRestored(Games.PlayerIdOf(2)), Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        Assert.Empty(transition.Effects);
        Assert.Equal(
            [new Player(Games.PlayerIdOf(1), "Zoé", IsConnected: false), new Player(Games.PlayerIdOf(2), "Max", IsConnected: true)],
            transition.State.Players);
        Assert.Equal(state.PlayerTokens, transition.State.PlayerTokens);
    }

    [Fact]
    public void Handle_PlayerConnectionRestoredDuringARound_MarksPlayerConnectedAgain()
    {
        // Given
        var state = Games.InPhase(GamePhase.Round, "Zoé", "Max");
        state = Games.Engine.Handle(state, new PlayerConnectionLost(Games.PlayerIdOf(1)), Games.Context()).State;

        // When
        var transition = Games.Engine.Handle(state, new PlayerConnectionRestored(Games.PlayerIdOf(1)), Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        Assert.Equal(GamePhase.Round, transition.State.Phase);
        Assert.True(transition.State.Players[0].IsConnected);
    }

    [Fact]
    public void Handle_PlayerConnectionRestoredWhileConnected_IsRejectedAsAlreadyConnected()
    {
        // Given
        var state = Games.LobbyWith("Zoé");

        // When
        var transition = Games.Engine.Handle(state, new PlayerConnectionRestored(Games.PlayerIdOf(1)), Games.Context());

        // Then
        Assert.Same(state, transition.State);
        Assert.Empty(transition.Effects);
        Assert.Equal(RejectionReason.PlayerAlreadyConnected, transition.Rejection);
    }

    [Fact]
    public void Handle_PlayerConnectionRestoredOfUnknownPlayer_IsRejected()
    {
        // Given
        var state = Games.LobbyWith("Zoé");

        // When
        var transition = Games.Engine.Handle(state, new PlayerConnectionRestored(Games.PlayerIdOf(9)), Games.Context());

        // Then
        Assert.Same(state, transition.State);
        Assert.Empty(transition.Effects);
        Assert.Equal(RejectionReason.PlayerUnknown, transition.Rejection);
    }
}
