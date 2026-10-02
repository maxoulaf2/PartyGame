using PartyGame.Contracts;
using PartyGame.Engine.Inputs;
using PartyGame.Engine.Tests.Rounds;

namespace PartyGame.Engine.Tests.Projections;

public sealed class SnapshotsTests
{
    public static TheoryData<GamePhase, Phase> Phases => new()
    {
        { GamePhase.Lobby, Phase.Lobby },
        { GamePhase.Round, Phase.Round },
        { GamePhase.BetweenRounds, Phase.BetweenRounds },
        { GamePhase.Finished, Phase.Finished },
    };

    [Fact]
    public void ForDisplay_LobbyWithPlayers_ShowsJoinAddressAndPlayersInOrderOfArrival()
    {
        // Given
        var state = Games.LobbyWith("Zoé", "Max", "Léa") with { Version = 7 };

        // When
        var snapshot = Games.Snapshots.ForDisplay(state);

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
        var snapshot = Games.Snapshots.ForDisplay(state);

        // Then
        Assert.Equal([("Zoé", false), ("Max", true)], snapshot.Players.Select(p => (p.Nickname, p.IsConnected)));
    }

    [Fact]
    public void ForDisplay_NoJoinAddress_ShowsNone()
    {
        // Given
        var state = Games.LobbyWith("Zoé") with { JoinAddress = null };

        // When
        var snapshot = Games.Snapshots.ForDisplay(state);

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
        var snapshot = Games.Snapshots.ForGameMaster(state);

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
        var snapshot = Games.Snapshots.ForGameMaster(state);

        // Then
        Assert.Equal(1, snapshot.MinimumPlayerCount);
    }

    [Fact]
    public void ForGameMaster_SeveralCandidates_ShowsTheAdvertisedAddressAndEveryCandidate()
    {
        // Given
        var state = Games.NewLobby();

        // When
        var snapshot = Games.Snapshots.ForGameMaster(state);

        // Then
        Assert.Equal(Games.JoinAddress, snapshot.JoinAddress);
        Assert.Equal(
            [new GameMasterJoinAddress(Games.JoinAddress, "Wi-Fi"), new GameMasterJoinAddress(Games.OtherAddress, "Ethernet")],
            snapshot.JoinAddressCandidates);
    }

    [Fact]
    public void ForDisplayAndGameMaster_AddressChosen_ShowTheChosenAddress()
    {
        // Given
        var state = Games.LobbyWith("Zoé");

        // When
        state = Games.Engine.Handle(state, Games.ChooseAddress(Games.OtherAddress), Games.Context()).State;

        // Then
        Assert.Equal(Games.OtherAddress, Games.Snapshots.ForDisplay(state).JoinAddress);
        Assert.Equal(Games.OtherAddress, Games.Snapshots.ForGameMaster(state).JoinAddress);
    }

    [Theory]
    [MemberData(nameof(Phases))]
    public void ForDisplayAndPlayer_AnyPhase_ContainNoOtherCandidateNorInterfaceName(GamePhase phase, Phase _)
    {
        // Given: only the game master may see the networks of the host
        var state = Games.InPhase(phase, "Zoé", "Max");

        // When
        var projections = new List<object> { Games.Snapshots.ForDisplay(state) };
        projections.AddRange(state.Players.Select(p => Games.Snapshots.ForPlayer(state, p)));

        // Then
        foreach (var json in projections.Select(Serialize))
        {
            Assert.DoesNotContain(Games.OtherAddress, json, StringComparison.Ordinal);
            Assert.DoesNotContain("Wi-Fi", json, StringComparison.Ordinal);
            Assert.DoesNotContain("Ethernet", json, StringComparison.Ordinal);
            Assert.DoesNotContain("candidate", json, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void ForEachRole_RenamedPlayer_ShowsTheNewNickname()
    {
        // Given
        var state = Games.LobbyWith("Zoé", "Max");

        // When
        state = Games.Engine.Handle(state, Games.Rename(2, "Maxime"), Games.Context()).State;

        // Then
        Assert.Equal(["Zoé", "Maxime"], Games.Snapshots.ForDisplay(state).Players.Select(p => p.Nickname));
        Assert.Equal(["Zoé", "Maxime"], Games.Snapshots.ForGameMaster(state).Players.Select(p => p.Nickname));
        Assert.Equal("Maxime", Games.Snapshots.ForPlayer(state, state.Players[1]).Nickname);
    }

    [Fact]
    public void ForPlayer_LobbyWithPlayers_ShowsThisPlayerAndPlayerCount()
    {
        // Given
        var state = Games.LobbyWith("Zoé", "Max") with { Version = 7 };

        // When
        var snapshot = Games.Snapshots.ForPlayer(state, state.Players[1]);

        // Then
        Assert.Equal(
            new PlayerSnapshot(state.GameId, 7, Phase.Lobby, Games.PlayerIdOf(2), "Max", PlayerCount: 2, Round: null, RoundView: null),
            snapshot);
    }

    [Theory]
    [MemberData(nameof(Phases))]
    public void ForEachRole_AnyPhase_ShowsThatPhase(GamePhase phase, Phase expected)
    {
        // Given
        var state = Games.InPhase(phase, "Zoé");

        // When
        Phase[] projected =
        [
            Games.Snapshots.ForDisplay(state).Phase,
            Games.Snapshots.ForGameMaster(state).Phase,
            Games.Snapshots.ForPlayer(state, state.Players[0]).Phase,
        ];

        // Then
        Assert.All(projected, p => Assert.Equal(expected, p));
    }

    [Theory]
    [MemberData(nameof(Phases))]
    public void ForEachRole_AnyPhase_ContainsNoPlayerToken(GamePhase phase, Phase _)
    {
        // Given
        var state = Games.InPhase(phase, "Zoé", "Max", "Léa");

        // When
        var projections = new List<object> { Games.Snapshots.ForDisplay(state), Games.Snapshots.ForGameMaster(state) };
        projections.AddRange(state.Players.Select(p => Games.Snapshots.ForPlayer(state, p)));

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
        var state = Games.InPhase(phase, "Zoé", "Max", "Léa");
        var player = state.Players[1];

        // When
        var json = Serialize(Games.Snapshots.ForPlayer(state, player));

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
        var state = Games.InPhase(phase, "Zoé", "Max");
        var withoutSecrets = state with { PlayerTokens = state.PlayerTokens.Clear(), JoinAddressCandidates = [] };

        // When
        var snapshot = Games.Snapshots.ForDisplay(state);

        // Then: compared as JSON, since the list of players has no value equality
        Assert.Equal(Serialize(Games.Snapshots.ForDisplay(withoutSecrets)), Serialize(snapshot));
    }

    [Theory]
    [MemberData(nameof(Phases))]
    public void ForGameMaster_AnyPhase_IsTheSameAsWithoutSecrets(GamePhase phase, Phase _)
    {
        // Given: the tokens are the only secret the game master may not see
        var state = Games.InPhase(phase, "Zoé", "Max");
        var withoutSecrets = state with { PlayerTokens = state.PlayerTokens.Clear() };

        // When
        var snapshot = Games.Snapshots.ForGameMaster(state);

        // Then
        Assert.Equal(Serialize(Games.Snapshots.ForGameMaster(withoutSecrets)), Serialize(snapshot));
    }

    [Fact]
    public void ForEachRole_Lobby_ShowsNoRound()
    {
        // Given
        var state = Games.InPhase(GamePhase.Lobby, "Zoé");

        // When
        (RoundInfo? Round, object? View)[] projected =
        [
            (Games.Snapshots.ForDisplay(state).Round, Games.Snapshots.ForDisplay(state).RoundView),
            (Games.Snapshots.ForGameMaster(state).Round, Games.Snapshots.ForGameMaster(state).RoundView),
            (Games.Snapshots.ForPlayer(state, state.Players[0]).Round, Games.Snapshots.ForPlayer(state, state.Players[0]).RoundView),
        ];

        // Then
        Assert.All(projected, p => Assert.Equal((null, null), p));
    }

    [Fact]
    public void ForEachRole_Round_ShowsTheRoundAndTheViewOfItsMode()
    {
        // Given
        var state = Games.InPhase(GamePhase.Round, "Zoé", "Max");
        state = Games.Accepted(state, Games.PlayerActs(state, player: 1, "answers A"));
        var expected = new RoundInfo(state.CurrentRound!.Id, Number: 1, Count: 2, "Échauffement");

        // When
        var display = Games.Snapshots.ForDisplay(state);
        var gameMaster = Games.Snapshots.ForGameMaster(state);
        var player = Games.Snapshots.ForPlayer(state, state.Players[1]);

        // Then
        Assert.Equal((expected, new FakeDisplayView("Échauffement", 2)), (display.Round, display.RoundView));
        Assert.Equal(expected, gameMaster.Round);
        Assert.Equal(
            ["start with 2 players", $"player {Games.PlayerIdOf(1).Value} answers A"],
            Assert.IsType<FakeGameMasterView>(gameMaster.RoundView).Inputs);
        Assert.Equal((expected, new FakePlayerView("Max", 2)), (player.Round, player.RoundView));
    }

    [Fact]
    public void ForPlayer_PlayerWhoJoinedDuringTheRound_ShowsTheViewOfTheRound()
    {
        // Given
        var state = Games.InPhase(GamePhase.Round, "Zoé");
        state = Games.Accepted(state, Games.Join("Max", player: 2));

        // When
        var snapshot = Games.Snapshots.ForPlayer(state, state.Players[1]);

        // Then
        Assert.Equal(new FakePlayerView("Max", 1), snapshot.RoundView);
        Assert.Equal(state.CurrentRound!.Id, snapshot.Round!.RoundId);
    }

    [Theory]
    [InlineData(GamePhase.BetweenRounds, 1, "Échauffement")]
    [InlineData(GamePhase.Finished, 2, "Finale")]
    public void ForEachRole_OutsideARound_ShowsTheLastRoundWithoutView(GamePhase phase, int number, string title)
    {
        // Given
        var state = Games.InPhase(phase, "Zoé");
        var expected = new RoundInfo(state.CurrentRound!.Id, number, Count: 2, title);

        // When
        (RoundInfo? Round, object? View)[] projected =
        [
            (Games.Snapshots.ForDisplay(state).Round, Games.Snapshots.ForDisplay(state).RoundView),
            (Games.Snapshots.ForGameMaster(state).Round, Games.Snapshots.ForGameMaster(state).RoundView),
            (Games.Snapshots.ForPlayer(state, state.Players[0]).Round, Games.Snapshots.ForPlayer(state, state.Players[0]).RoundView),
        ];

        // Then
        Assert.All(projected, p => Assert.Equal((expected, null), p));
    }

    private static string Serialize(object snapshot) => FakeJson.Serialize(snapshot);
}
