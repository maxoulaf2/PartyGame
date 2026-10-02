using System.Text.Json;
using PartyGame.Contracts;
using PartyGame.Contracts.Serialization;
using PartyGame.Engine.Inputs;
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
    public void ForDisplay_LobbyWithPlayers_ShowsJoinAddressAndPlayersInOrderOfArrival()
    {
        // Given
        var state = Games.LobbyWith("Zoé", "Max", "Léa") with { Version = 7 };

        // When
        var snapshot = Snapshots.ForDisplay(state);

        // Then
        Assert.Equal(state.GameId, snapshot.GameId);
        Assert.Equal(7, snapshot.Version);
        Assert.Equal(Phase.Lobby, snapshot.Phase);
        Assert.Equal(Games.JoinAddress, snapshot.JoinAddress);
        Assert.Equal(
            [
                new DisplayPlayer(Games.PlayerIdOf(1), "Zoé", IsConnected: true),
                new DisplayPlayer(Games.PlayerIdOf(2), "Max", IsConnected: true),
                new DisplayPlayer(Games.PlayerIdOf(3), "Léa", IsConnected: true),
            ],
            snapshot.Players);
    }

    [Fact]
    public void ForDisplay_DisconnectedPlayer_StaysListedAsDisconnected()
    {
        // Given
        var state = Games.LobbyWith("Zoé", "Max");
        state = Games.Engine.Handle(state, new PlayerConnectionLost(Games.PlayerIdOf(1)), Games.Context()).State;

        // When
        var snapshot = Snapshots.ForDisplay(state);

        // Then
        Assert.Equal([("Zoé", false), ("Max", true)], snapshot.Players.Select(p => (p.Nickname, p.IsConnected)));
    }

    [Fact]
    public void ForDisplay_NoJoinAddress_ShowsNone()
    {
        // Given
        var state = Games.LobbyWith("Zoé") with { JoinAddress = null };

        // When
        var snapshot = Snapshots.ForDisplay(state);

        // Then
        Assert.Null(snapshot.JoinAddress);
        Assert.Single(snapshot.Players);
    }

    [Fact]
    public void ForGameMaster_LobbyWithPlayers_ShowsGameVersionPhaseAndPlayersInOrderOfArrival()
    {
        // Given
        var state = Games.LobbyWith("Zoé", "Max", "Léa") with { Version = 7 };
        state = Games.Engine.Handle(state, new PlayerConnectionLost(Games.PlayerIdOf(2)), Games.Context()).State;

        // When
        var snapshot = Snapshots.ForGameMaster(state);

        // Then
        Assert.Equal((state.GameId, 7, Phase.Lobby), (snapshot.GameId, snapshot.Version, snapshot.Phase));
        Assert.Equal(
            [
                new GameMasterPlayer(Games.PlayerIdOf(1), "Zoé", IsConnected: true),
                new GameMasterPlayer(Games.PlayerIdOf(2), "Max", IsConnected: false),
                new GameMasterPlayer(Games.PlayerIdOf(3), "Léa", IsConnected: true),
            ],
            snapshot.Players);
    }

    [Fact]
    public void ForGameMaster_AnyState_ShowsTheMinimumPlayerCountToStart()
    {
        // Given
        var state = Games.NewLobby();

        // When
        var snapshot = Snapshots.ForGameMaster(state);

        // Then
        Assert.Equal(1, snapshot.MinimumPlayerCount);
    }

    [Fact]
    public void ForEachRole_RenamedPlayer_ShowsTheNewNickname()
    {
        // Given
        var state = Games.LobbyWith("Zoé", "Max");

        // When
        state = Games.Engine.Handle(state, Games.Rename(2, "Maxime"), Games.Context()).State;

        // Then
        Assert.Equal(["Zoé", "Maxime"], Snapshots.ForDisplay(state).Players.Select(p => p.Nickname));
        Assert.Equal(["Zoé", "Maxime"], Snapshots.ForGameMaster(state).Players.Select(p => p.Nickname));
        Assert.Equal("Maxime", Snapshots.ForPlayer(state, state.Players[1]).Nickname);
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

        // Then: compared as JSON, since the list of players has no value equality
        Assert.Equal(Serialize(Snapshots.ForDisplay(withoutSecrets)), Serialize(snapshot));
    }

    [Theory]
    [MemberData(nameof(Phases))]
    public void ForGameMaster_AnyPhase_IsTheSameAsWithoutSecrets(GamePhase phase, Phase _)
    {
        // Given: the tokens are the only secret the game master may not see
        var state = Games.LobbyWith("Zoé", "Max") with { Phase = phase };
        var withoutSecrets = state with { PlayerTokens = state.PlayerTokens.Clear() };

        // When
        var snapshot = Snapshots.ForGameMaster(state);

        // Then
        Assert.Equal(Serialize(Snapshots.ForGameMaster(withoutSecrets)), Serialize(snapshot));
    }

    private static string Serialize(object snapshot) =>
        JsonSerializer.Serialize(snapshot, snapshot.GetType(), ContractJsonOptions.Default);
}
