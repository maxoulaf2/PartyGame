using PartyGame.Contracts;
using PartyGame.Contracts.Packs;
using PartyGame.Engine.Inputs;
using PartyGame.Engine.Tests.Rounds;

namespace PartyGame.Engine.Tests.Projections;

public sealed class SnapshotsTests
{
    public static TheoryData<GamePhase, Phase> Phases => new()
    {
        { GamePhase.Lobby, Phase.Lobby },
        { GamePhase.RoundIntro, Phase.RoundIntro },
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
                new GameMasterPlayer(Games.PlayerIdOf(1), "Zoé", IsConnected: true, Score: 0, "CODE01"),
                new GameMasterPlayer(Games.PlayerIdOf(2), "Max", IsConnected: false, Score: 0, "CODE02"),
                new GameMasterPlayer(Games.PlayerIdOf(3), "Léa", IsConnected: true, Score: 0, "CODE03"),
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

    [Fact]
    public void ForGameMaster_Lobby_ShowsEveryPackWithTheRoundsOfTheValidOnesAndTheProblemsOfTheInvalidOnes()
    {
        // Given
        var invalid = Games.InvalidPack("casse");
        var state = Games.Accepted(Games.LobbyWith("Zoé"), Games.Loaded(invalid, Games.Pack));

        // When
        var snapshot = Games.Snapshots.ForGameMaster(state);

        // Then
        var catalog = Assert.IsType<GameMasterPackCatalog>(snapshot.PackCatalog);
        Assert.Equal(Games.PackDirectory, catalog.Directory);
        Assert.Equal(2, catalog.Packs.Length);
        var shownInvalid = catalog.Packs[0];
        Assert.Equal(("casse", "Pack cassé", 1, false), (shownInvalid.Id, shownInvalid.Title, shownInvalid.RoundCount, shownInvalid.IsValid));
        Assert.Empty(shownInvalid.Rounds);
        Assert.Equal(invalid.Problems, shownInvalid.Problems);
        var shownValid = catalog.Packs[1];
        Assert.Equal((Games.PackId, "Soirée test", 2, true), (shownValid.Id, shownValid.Title, shownValid.RoundCount, shownValid.IsValid));
        Assert.Equal([new GameMasterPackRound("Échauffement", ""), new GameMasterPackRound("Finale", "")], shownValid.Rounds);
        Assert.Empty(shownValid.Problems);
        Assert.Equal((Games.PackId, "Soirée test"), (snapshot.SelectedPackId, snapshot.PackTitle));
    }

    [Fact]
    public void ForGameMaster_QuizRound_ShowsTheTypeOfTheActivityAsItsMode()
    {
        // Given: the fake activities of the tests declare no type
        var quiz = Games.ValidPack("quiz", "Quiz", [new QuizRoundDescriptor { Title = "Culture", Questions = [] }]);
        var state = Games.Accepted(Games.LobbyWith("Zoé"), Games.Loaded(quiz));

        // When
        var snapshot = Games.Snapshots.ForGameMaster(state);

        // Then
        Assert.Equal([new GameMasterPackRound("Culture", "quiz")], Assert.Single(snapshot.PackCatalog!.Packs).Rounds);
    }

    [Fact]
    public void ForGameMasterAndDisplay_NoPackChosen_ShowNoTitle()
    {
        // Given
        var state = Games.Accepted(Games.LobbyWith("Zoé"), Games.Loaded(Games.InvalidPack("casse")));

        // When
        var gameMaster = Games.Snapshots.ForGameMaster(state);
        var display = Games.Snapshots.ForDisplay(state);

        // Then
        Assert.Equal((null, null), (gameMaster.SelectedPackId, gameMaster.PackTitle));
        Assert.Null(display.PackTitle);
    }

    [Theory]
    [InlineData(GamePhase.Round)]
    [InlineData(GamePhase.BetweenRounds)]
    [InlineData(GamePhase.Finished)]
    public void ForGameMaster_OnceStarted_ShowsThePackOfTheGameButNoCatalog(GamePhase phase)
    {
        // Given
        var state = Games.InPhase(phase, "Zoé");

        // When
        var snapshot = Games.Snapshots.ForGameMaster(state);

        // Then
        Assert.Null(snapshot.PackCatalog);
        Assert.Equal((Games.PackId, "Soirée test"), (snapshot.SelectedPackId, snapshot.PackTitle));
    }

    [Theory]
    [MemberData(nameof(Phases))]
    public void ForDisplay_AnyPhase_ShowsTheTitleOfThePackOfTheGame(GamePhase phase, Phase _)
    {
        // Given
        var state = Games.InPhase(phase, "Zoé");

        // When
        var snapshot = Games.Snapshots.ForDisplay(state);

        // Then
        Assert.Equal("Soirée test", snapshot.PackTitle);
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
            new PlayerSnapshot(state.GameId, 7, Phase.Lobby, Games.PlayerIdOf(2), "Max", Score: 0, PlayerCount: 2, Round: null, RoundView: null, Standing: null, FinishedAt: null, PausedAt: null),
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
        var expected = new RoundInfo(state.CurrentRound!.Id, Number: 1, Count: 2, "Échauffement", Mode: "", Description: null);

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
    public void ForEachRole_RoundIntro_ShowsTheRoundAnnouncedWithItsModeAndDescriptionButNoView()
    {
        // Given
        var pack = Games.ValidPack(
            "decrite",
            "Soirée décrite",
            [new FakeRoundDescriptor { Title = "Échauffement" }, new FakeRoundDescriptor { Title = "Finale", Description = "Tout se joue ici." }]);
        var lobby = Games.Accepted(Games.Accepted(Games.LobbyWith("Zoé"), Games.Loaded(Games.Pack, pack)), Games.Select(pack.Id));
        var state = Games.PlayedUpTo(GamePhase.BetweenRounds, lobby);
        state = Games.Accepted(state, Games.NextRound(state), seed: 43);
        var expected = new RoundInfo(state.CurrentRound!.Id, Number: 2, Count: 2, "Finale", Mode: "", "Tout se joue ici.");

        // When
        (RoundInfo? Round, object? View)[] projected =
        [
            (Games.Snapshots.ForDisplay(state).Round, Games.Snapshots.ForDisplay(state).RoundView),
            (Games.Snapshots.ForGameMaster(state).Round, Games.Snapshots.ForGameMaster(state).RoundView),
            (Games.Snapshots.ForPlayer(state, state.Players[0]).Round, Games.Snapshots.ForPlayer(state, state.Players[0]).RoundView),
        ];

        // Then
        Assert.All(projected, p => Assert.Equal((expected, null), p));
        Assert.Null(Games.Snapshots.ForGameMaster(state).NextRoundTitle);
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
        var expected = new RoundInfo(state.CurrentRound!.Id, number, Count: 2, title, Mode: "", Description: null);

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

    [Fact]
    public void ForDisplay_RoundWithAnImage_ShowsTheUrlOfItsMedia()
    {
        // Given
        var state = Games.PlayedUpTo(GamePhase.Round, Games.IllustratedLobbyWith("Zoé"));
        var id = state.Media.Files.Single(f => f.Value == Games.Flag).Key;

        // When
        var snapshot = Games.Snapshots.ForDisplay(state);

        // Then
        Assert.Equal($"/media/{id.Value}", Assert.IsType<FakeDisplayView>(snapshot.RoundView).ImageUrl);
    }

    [Theory]
    [InlineData(GamePhase.BetweenRounds)]
    [InlineData(GamePhase.Finished)]
    public void ForGameMaster_RoundSkipped_TellsIt(GamePhase phase)
    {
        // Given: the first round skipped, or the last one
        var state = phase == GamePhase.BetweenRounds
            ? Games.InPhase(GamePhase.Round, "Zoé")
            : Games.InLastRound("Zoé");
        state = Games.Accepted(state, Games.SkipRound(state));
        Assert.Equal(phase, state.Phase);

        // When
        var snapshot = Games.Snapshots.ForGameMaster(state);

        // Then
        Assert.True(snapshot.RoundSkipped);
    }

    [Theory]
    [MemberData(nameof(Phases))]
    public void ForGameMaster_NoRoundSkipped_DoesNotTellOne(GamePhase phase, Phase _)
    {
        // Given: every round ended by its mode
        var state = Games.InPhase(phase, "Zoé");

        // When
        var snapshot = Games.Snapshots.ForGameMaster(state);

        // Then
        Assert.False(snapshot.RoundSkipped);
    }
}
