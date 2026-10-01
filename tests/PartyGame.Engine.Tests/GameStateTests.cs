using System.Text.Json;
using PartyGame.Contracts;
using PartyGame.Contracts.Serialization;

namespace PartyGame.Engine.Tests;

public sealed class GameStateTests
{
    [Fact]
    public void Create_NewGame_IsEmptyLobbyWithItsIdAtVersionOne()
    {
        // Given
        var gameId = new GameId(Guid.NewGuid());

        // When
        var state = GameState.Create(gameId);

        // Then
        Assert.Equal(gameId, state.GameId);
        Assert.Equal(1, state.Version);
        Assert.Equal(GamePhase.Lobby, state.Phase);
        Assert.Empty(state.Players);
        Assert.Empty(state.PlayerTokens);
    }

    [Fact]
    public void Serialize_StateWithPlayers_RoundTripsUnchanged()
    {
        // Given
        var state = Games.LobbyWith("Zoé", "Max") with { Phase = GamePhase.Started, Version = 42 };

        // When
        var json = JsonSerializer.Serialize(state, ContractJsonOptions.Default);
        var restored = JsonSerializer.Deserialize<GameState>(json, ContractJsonOptions.Default)!;

        // Then
        Assert.Equal(state.GameId, restored.GameId);
        Assert.Equal(state.Version, restored.Version);
        Assert.Equal(state.Phase, restored.Phase);
        Assert.Equal(state.Players, restored.Players);
        Assert.Equal(state.PlayerTokens.OrderBy(t => t.Key.Value), restored.PlayerTokens.OrderBy(t => t.Key.Value));
    }
}
