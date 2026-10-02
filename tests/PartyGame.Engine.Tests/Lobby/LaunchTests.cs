using PartyGame.Engine.Inputs;

namespace PartyGame.Engine.Tests.Lobby;

public sealed class LaunchTests
{
    [Fact]
    public void Handle_StartGameWithPlayers_StartsTheFirstRoundAndKeepsThePlayers()
    {
        // Given
        var state = Games.InPhase(GamePhase.Lobby, "Zoé", "Max");

        // When
        var transition = Games.Engine.Handle(state, Games.Start(), Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        Assert.Equal(GamePhase.Round, transition.State.Phase);
        Assert.Equal(0, transition.State.CurrentRound!.Index);
        Assert.Equal(state.Players, transition.State.Players);
        Assert.Equal(state.PlayerTokens, transition.State.PlayerTokens);
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
        Assert.Equal(GamePhase.Round, transition.State.Phase);
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
}
