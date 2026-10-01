using System.Text.Json;
using PartyGame.Contracts.Serialization;
using PartyGame.Engine.Inputs;

namespace PartyGame.Engine.Tests;

public sealed class GameEngineTests
{
    [Fact]
    public void Handle_AcceptedInput_ReturnsNewStateAndLeavesGivenStateUntouched()
    {
        // Given
        var state = Games.NewLobby();

        // When
        var transition = Games.Engine.Handle(state, Games.Join("Zoé"), Games.Context());

        // Then
        Assert.NotSame(state, transition.State);
        Assert.Null(transition.Rejection);
        Assert.Empty(state.Players);
        Assert.Empty(state.PlayerTokens);
    }

    [Fact]
    public void Handle_TimerElapsedInLobby_IsRejectedAsUnexpected()
    {
        // Given
        var state = Games.LobbyWith("Zoé");

        // When
        var transition = Games.Engine.Handle(state, new TimerElapsed(new TimerId("countdown"), DateTimeOffset.UnixEpoch), Games.Context());

        // Then
        Assert.Same(state, transition.State);
        Assert.Empty(transition.Effects);
        Assert.Equal(RejectionReason.UnexpectedTimer, transition.Rejection);
    }

    [Fact]
    public void Handle_SameStateInputAndContext_ProducesSameResult()
    {
        // Given
        var state = Games.LobbyWith("Zoé");
        var input = Games.Join("Max", player: 2);

        // When
        var first = Games.Engine.Handle(state, input, Games.Context(seed: 7));
        var second = Games.Engine.Handle(state, input, Games.Context(seed: 7));

        // Then
        Assert.Equal(
            JsonSerializer.Serialize(first.State, ContractJsonOptions.Default),
            JsonSerializer.Serialize(second.State, ContractJsonOptions.Default));
        Assert.Equal(first.Effects, second.Effects);
        Assert.Equal(first.Rejection, second.Rejection);
    }

    [Fact]
    public void Handle_UnknownInputType_Throws()
    {
        // Given
        var state = Games.NewLobby();

        // When
        var handle = () => Games.Engine.Handle(state, new UnknownInput(), Games.Context());

        // Then
        Assert.Throws<NotSupportedException>(handle);
    }

    private sealed record UnknownInput : GameInput;
}
