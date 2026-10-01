using System.Text.Json;
using PartyGame.Contracts;
using PartyGame.Contracts.Serialization;
using PartyGame.Engine.Projections;

namespace PartyGame.Engine.Tests.Projections;

public sealed class SnapshotsTests
{
    public static TheoryData<GamePhase, Phase> Phases => new()
    {
        { GamePhase.Lobby, Phase.Lobby },
        { GamePhase.Started, Phase.Started },
    };

    [Fact]
    public void ForDisplay_LobbyWithPlayers_ShowsGameVersionPhaseAndPlayerCount()
    {
        // Given
        var state = Games.LobbyWith("Zoé", "Max") with { Version = 7 };

        // When
        var snapshot = Snapshots.ForDisplay(state);

        // Then
        Assert.Equal(new DisplaySnapshot(state.GameId, 7, Phase.Lobby, PlayerCount: 2), snapshot);
    }

    [Fact]
    public void ForGameMaster_LobbyWithPlayers_ShowsGameVersionPhaseAndPlayerCount()
    {
        // Given
        var state = Games.LobbyWith("Zoé", "Max") with { Version = 7 };

        // When
        var snapshot = Snapshots.ForGameMaster(state);

        // Then
        Assert.Equal(new GameMasterSnapshot(state.GameId, 7, Phase.Lobby, PlayerCount: 2), snapshot);
    }

    [Fact]
    public void ForPlayer_LobbyWithPlayers_ShowsThisPlayerAndPlayerCount()
    {
        // Given
        var state = Games.LobbyWith("Zoé", "Max") with { Version = 7 };

        // When
        var snapshot = Snapshots.ForPlayer(state, state.Players[1]);

        // Then
        Assert.Equal(new PlayerSnapshot(state.GameId, 7, Phase.Lobby, Games.PlayerIdOf(2), "Max", PlayerCount: 2), snapshot);
    }

    [Theory]
    [MemberData(nameof(Phases))]
    public void ForEachRole_AnyPhase_ShowsThatPhase(GamePhase phase, Phase expected)
    {
        // Given
        var state = Games.LobbyWith("Zoé") with { Phase = phase };

        // When
        Phase[] projected =
        [
            Snapshots.ForDisplay(state).Phase,
            Snapshots.ForGameMaster(state).Phase,
            Snapshots.ForPlayer(state, state.Players[0]).Phase,
        ];

        // Then
        Assert.All(projected, p => Assert.Equal(expected, p));
    }

    [Theory]
    [MemberData(nameof(Phases))]
    public void ForEachRole_AnyPhase_ContainsNoPlayerToken(GamePhase phase, Phase _)
    {
        // Given
        var state = Games.LobbyWith("Zoé", "Max", "Léa") with { Phase = phase };

        // When
        var projections = new List<object> { Snapshots.ForDisplay(state), Snapshots.ForGameMaster(state) };
        projections.AddRange(state.Players.Select(p => Snapshots.ForPlayer(state, p)));

        // Then
        foreach (var json in projections.Select(Serialize))
        {
            Assert.All(state.PlayerTokens.Keys, token => Assert.DoesNotContain(token.Value, json, StringComparison.Ordinal));
        }
    }

    [Theory]
    [MemberData(nameof(Phases))]
    public void ForPlayer_AnyPhase_ContainsNothingAboutOtherPlayers(GamePhase phase, Phase _)
    {
        // Given
        var state = Games.LobbyWith("Zoé", "Max", "Léa") with { Phase = phase };
        var player = state.Players[1];

        // When
        var json = Serialize(Snapshots.ForPlayer(state, player));

        // Then
        foreach (var other in state.Players.Where(p => p != player))
        {
            Assert.DoesNotContain(other.Nickname, json, StringComparison.Ordinal);
            Assert.DoesNotContain(other.Id.Value.ToString(), json, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Theory]
    [MemberData(nameof(Phases))]
    public void ForDisplay_AnyPhase_IsTheSameAsWithoutSecrets(GamePhase phase, Phase _)
    {
        // Given: everything the TV screen may not show is removed from the state
        var state = Games.LobbyWith("Zoé", "Max") with { Phase = phase };
        var withoutSecrets = state with { PlayerTokens = state.PlayerTokens.Clear() };

        // When
        var snapshot = Snapshots.ForDisplay(state);

        // Then
        Assert.Equal(Snapshots.ForDisplay(withoutSecrets), snapshot);
    }

    private static string Serialize(object snapshot) =>
        JsonSerializer.Serialize(snapshot, snapshot.GetType(), ContractJsonOptions.Default);
}
