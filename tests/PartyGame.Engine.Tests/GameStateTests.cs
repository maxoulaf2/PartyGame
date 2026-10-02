using System.Text.Json;
using PartyGame.Contracts;
using PartyGame.Engine.Tests.Rounds;

namespace PartyGame.Engine.Tests;

public sealed class GameStateTests
{
    [Fact]
    public void Create_NewGame_IsEmptyLobbyWithItsIdAddressAndPacksAtVersionOne()
    {
        // Given
        var gameId = new GameId(Guid.NewGuid());
        var catalog = new PackCatalog(Games.PackDirectory, [Games.Pack, Games.ValidPack("autre", "Autre", Games.TwoRounds)]);

        // When
        var state = GameState.Create(gameId, "192.168.1.42", Games.JoinAddressCandidates, catalog);

        // Then
        Assert.Equal(gameId, state.GameId);
        Assert.Equal(1, state.Version);
        Assert.Equal(GamePhase.Lobby, state.Phase);
        Assert.Equal("192.168.1.42", state.JoinAddress);
        Assert.Equal(Games.JoinAddressCandidates, state.JoinAddressCandidates);
        Assert.Empty(state.Players);
        Assert.Empty(state.PlayerTokens);
        Assert.Same(catalog, state.Catalog);
        Assert.Null(state.SelectedPackId);
        Assert.Null(state.Pack);
        Assert.Empty(state.Rounds);
        Assert.Empty(state.Media.Files);
        Assert.Null(state.CurrentRound);
    }

    [Fact]
    public void Create_SingleValidPack_ChoosesIt()
    {
        // Given: the game master has nothing to choose
        var catalog = new PackCatalog(Games.PackDirectory, [Games.InvalidPack("a-casse"), Games.Pack]);

        // When
        var state = GameState.Create(new GameId(Guid.NewGuid()), "192.168.1.42", [], catalog);

        // Then
        Assert.Equal(Games.PackId, state.SelectedPackId);
        Assert.Null(state.Pack);
    }

    [Fact]
    public void Create_NoValidPack_ChoosesNone()
    {
        // Given
        var catalog = new PackCatalog(Games.PackDirectory, [Games.InvalidPack("a-casse")]);

        // When
        var state = GameState.Create(new GameId(Guid.NewGuid()), "192.168.1.42", [], catalog);

        // Then
        Assert.Null(state.SelectedPackId);
    }

    [Fact]
    public void Serialize_StartedGameWithPlayersAndPacks_RoundTripsUnchanged()
    {
        // Given
        var state = Games.PlayedUpTo(GamePhase.Round, Games.IllustratedLobbyWith("Zoé", "Max")) with { Version = 42 };
        state = state with { Catalog = state.Catalog with { Packs = [.. state.Catalog.Packs, Games.InvalidPack("casse")] } };

        // When
        var json = JsonSerializer.Serialize(state with { CurrentRound = null }, FakeJson.Options);
        var restored = JsonSerializer.Deserialize<GameState>(json, FakeJson.Options)!;

        // Then
        Assert.Equal(state.GameId, restored.GameId);
        Assert.Equal(state.Version, restored.Version);
        Assert.Equal(state.Phase, restored.Phase);
        Assert.Equal(state.JoinAddress, restored.JoinAddress);
        Assert.Equal(state.JoinAddressCandidates, restored.JoinAddressCandidates);
        Assert.Equal(state.Players, restored.Players);
        Assert.Equal(state.PlayerTokens.OrderBy(t => t.Key.Value), restored.PlayerTokens.OrderBy(t => t.Key.Value));
        Assert.Equal(state.SelectedPackId, restored.SelectedPackId);
        Assert.Equal(state.Rounds, restored.Rounds);
        Assert.Equal(
            state.Media.Files.OrderBy(f => f.Key.Value, StringComparer.Ordinal),
            restored.Media.Files.OrderBy(f => f.Key.Value, StringComparer.Ordinal));
        Assert.Equal(json, JsonSerializer.Serialize(restored, FakeJson.Options));
    }
}
