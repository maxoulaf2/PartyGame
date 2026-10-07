using System.IO.Compression;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using PartyGame.Contracts;
using PartyGame.Engine.State;
using PartyGame.Server.Games;
using PartyGame.Server.Hubs;
using PartyGame.Server.Packs;
using PartyGame.Server.Persistence;
using PartyGame.Server.Tests.Hubs;
using PartyGame.Server.Tests.Packs;
using PartyGame.Tests.Shared.Leaks;

namespace PartyGame.Server.Tests.Persistence;

/// <summary>
/// A server restarted on the data folder of a previous run, which offers the game master to resume the game it saved.
/// </summary>
public sealed class ResumeOfferTests : IAsyncDisposable
{
    private const string PreviousCode = "135790";
    private const string Code = "246802";
    private const string Image = "images/drapeau.png";

    private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(10);

    private readonly TempDirectory _data = new();
    private readonly TempDirectory _packs = new();
    private WebApplicationFactory<Program>? _factory;

    public ResumeOfferTests()
    {
        // The only pack of the directory, chosen at once, so that the game can start.
        TestPacks.Write(_packs.Path, "soiree", TestPacks.IllustratedQuiz("Soirée test", Image, Image));
        TestPacks.WriteMedia(_packs.Path, "soiree", Image, [1, 2, 3]);
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private GameLoop Game => _factory!.Services.GetRequiredService<GameLoop>();

    private string SaveFile => Path.Combine(_data.Path, GamePersistence.FileName);

    private string PreviousFile => Path.Combine(_data.Path, GamePersistence.PreviousFileName);

    private string MediaFile => Path.Combine(_packs.Path, "soiree", "images", "drapeau.png");

    public async ValueTask DisposeAsync()
    {
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }

        _data.Dispose();
        _packs.Dispose();
    }

    [Fact]
    public async Task Startup_GameSavedWithPlayers_WaitsForTheGameMasterWhoAloneLearnsWhereItStopped()
    {
        // Given
        var saved = await SaveGameAsync(started: true, "Zoé", "Max");

        // When
        Restart();
        await using var display = await HubClients.ConnectAsync(_factory!);
        using var toDisplay = new ReceivedSnapshots(display);
        await AnnounceAsync(display, Role.Display, code: null);
        await using var gameMaster = await ConnectGameMasterAsync();
        using var toGameMaster = new ReceivedSnapshots(gameMaster);
        await AnnounceAsync(gameMaster, Role.GameMaster, Code);

        // Then
        Assert.Equal(GamePhase.ResumePending, Game.State.Phase);
        await FlushAsync(display, gameMaster);
        var described = Assert.Single(toGameMaster.GameMaster).SavedGame!;
        Assert.Equal((saved.GameId, Phase.Round, "Soirée test", 2), (described.GameId, described.Phase, described.PackTitle, described.PlayerCount));
        Assert.Equal((1, 1, "Manche illustrée"), (described.Round!.Number, described.Round.Count, described.Round.Title));
        Assert.Equal(new RoundStep(1, 2), described.Step);
        Assert.Empty(described.MissingMedia);
        var shown = Assert.Single(toDisplay.Display);
        Assert.Equal((Phase.ResumePending, null), (shown.Phase, shown.PackTitle));
        LeakAssert.NoSecretReceived(
            Viewer.Display,
            toDisplay.Json,
            new Secret("Zoé", Audience.AllButGameMaster),
            new Secret("Soirée test", Audience.AllButGameMaster),
            new Secret("Manche illustrée", Audience.AllButGameMaster));
        LeakAssert.NoSecretReceived(Viewer.GameMaster, toGameMaster.Json, new Secret("Zoé", Audience.Everyone), new Secret(Image, Audience.Everyone));
    }

    [Fact]
    public async Task Startup_GameOfAZipPackWithItsCacheDeleted_ExtractsItAgainAndFindsEveryMedia()
    {
        // Given: the pack shared as a zip, a game saved with it, then the cache of the extracted zips deleted
        var folder = Path.Combine(_packs.Path, "soiree");
        ZipFile.CreateFromDirectory(folder, folder + ".zip");
        Directory.Delete(folder, recursive: true);
        await SaveGameAsync(started: true, "Zoé");
        Directory.Delete(Path.Combine(_data.Path, "packs-cache"), recursive: true);

        // When
        Restart();

        // Then
        Assert.Equal(GamePhase.ResumePending, Game.State.Phase);
        Assert.Empty(Game.State.PendingGame!.MissingMedia);
    }

    [Fact]
    public async Task Startup_KilledWhileWritingTheSave_OffersTheLastCompleteSave()
    {
        // Given: a game saved, then the server killed in the middle of the next write, which left its temporary file cut short
        var saved = await SaveGameAsync(started: true, "Zoé", "Max");
        var temporaryFile = Path.Combine(_data.Path, GamePersistence.TemporaryFileName);
        var complete = await File.ReadAllTextAsync(SaveFile, Ct);
        await File.WriteAllTextAsync(temporaryFile, complete.Replace($"\"version\":{saved.Version},", $"\"version\":{saved.Version + 1},", StringComparison.Ordinal)[..(complete.Length / 2)], Ct);

        // When
        Restart();

        // Then
        Assert.Equal(GamePhase.ResumePending, Game.State.Phase);
        var pending = Game.State.PendingGame!.Game;
        Assert.Equal((saved.GameId, saved.Version, 2), (pending.GameId, pending.Version, pending.Players.Length));
        Assert.False(File.Exists(temporaryFile));
        Assert.Equal(complete, await File.ReadAllTextAsync(SaveFile, Ct));
    }

    [Fact]
    public async Task Startup_GameSavedWithoutPlayer_StartsANewGame()
    {
        // Given: a game saved before anybody joined
        await SaveGameAsync(started: false, "Zoé");
        var json = JsonNode.Parse(await File.ReadAllTextAsync(SaveFile, Ct))!;
        json["game"]!["players"] = new JsonArray();
        json["game"]!["playerTokens"] = new JsonObject();
        await File.WriteAllTextAsync(SaveFile, json.ToJsonString(), Ct);

        // When
        Restart();

        // Then
        Assert.Equal(GamePhase.Lobby, Game.State.Phase);
        Assert.Null(Game.State.PendingGame);
    }

    [Theory]
    [InlineData("""{ "formatVersion": 1, "savedAt": "2026-10-04T21:00:00+00:00", "game": { "gameId": """)]
    [InlineData("not json")]
    [InlineData("""{ "formatVersion": 99, "savedAt": "2026-10-04T21:00:00+00:00", "game": null }""")]
    [InlineData("""{ "formatVersion": 1, "savedAt": "2026-10-04T21:00:00+00:00" }""")]
    public async Task Startup_SaveUnreadableOrOfAnUnknownFormat_SetsItAsideAndTellsTheGameMaster(string content)
    {
        // Given
        Directory.CreateDirectory(_data.Path);
        await File.WriteAllTextAsync(SaveFile, content, Ct);

        // When
        Restart();
        await using var gameMaster = await ConnectGameMasterAsync();
        using var toGameMaster = new ReceivedSnapshots(gameMaster);
        await AnnounceAsync(gameMaster, Role.GameMaster, Code);

        // Then: a new game, the file kept aside for whoever wants to look at it
        Assert.Equal(GamePhase.Lobby, Game.State.Phase);
        var aside = Assert.Single(Directory.GetFiles(_data.Path, "current-game.unreadable-*.json"));
        Assert.Equal(content, await File.ReadAllTextAsync(aside, Ct));
        Assert.False(File.Exists(SaveFile));
        await FlushAsync(gameMaster);
        Assert.Equal(IncidentCode.SavedGameUnreadable, Assert.Single(Assert.Single(toGameMaster.Incidents).Incidents).Code);
        Assert.Contains(LoggedEvent.ReadAll(_data), e => e.Level == "Warning" && e.Template.StartsWith("Saved game in {File} cannot be resumed", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ResolveSavedGame_Resume_GoesOnWithTheGameFoundAndWelcomesThePhonesBack()
    {
        // Given: a phone that waits for the decision
        var saved = await SaveGameAsync(started: true, "Zoé");
        Restart();
        var welcomes = new List<Welcome>();
        await using var zoe = await HubClients.ConnectAsync(_factory!, beforeStart: c => c.On<Welcome>(nameof(IGameClient.ReceiveWelcome), welcomes.Add));
        var waiting = await zoe.InvokeAsync<ResumeSessionResult>(GameHub.ResumeSession, new ResumeSessionRequest(saved.Tokens[0]), Ct);
        var recovering = await zoe.InvokeAsync<RecoverSessionResult>(GameHub.RecoverSession, new RecoverSessionRequest("ABCDEF"), Ct);
        await using var gameMaster = await ConnectGameMasterAsync();
        await AnnounceAsync(gameMaster, Role.GameMaster, Code);

        // When
        await gameMaster.InvokeAsync(GameHub.ResolveSavedGame, new ResolveSavedGameRequest(saved.GameId, Resume: true), Ct);

        // Then: the game goes on counting, its round resumed right after it, and the phone finds its place back with its token
        Assert.Equal(ResumeSessionRefusal.GamePending, waiting.Refusal);
        Assert.Equal(RecoverSessionRefusal.GamePending, recovering.Refusal);
        Assert.Equal((saved.GameId, saved.Version + 2, GamePhase.Round), (Game.State.GameId, Game.State.Version, Game.State.Phase));
        await FlushAsync(zoe);
        Assert.Equal([true, false], welcomes.Select(w => w.GamePending));
        using var toZoe = new ReceivedSnapshots(zoe);
        var resumed = await zoe.InvokeAsync<ResumeSessionResult>(GameHub.ResumeSession, new ResumeSessionRequest(saved.Tokens[0]), Ct);
        Assert.Null(resumed.Refusal);
        await FlushAsync(zoe);
        Assert.Equal(("Zoé", Phase.Round), (toZoe.Player[^1].Nickname, toZoe.Player[^1].Phase));
        await EventuallyAsync(() => SavedGameId() == saved.GameId && SavedVersion() > saved.Version);
    }

    [Fact]
    public async Task ResolveSavedGame_NewGame_SetsTheSaveAsideAndStartsALobbyThePhonesRegisterIn()
    {
        // Given
        var saved = await SaveGameAsync(started: true, "Zoé");
        Restart();
        await using var gameMaster = await ConnectGameMasterAsync();
        await AnnounceAsync(gameMaster, Role.GameMaster, Code);

        // When
        await gameMaster.InvokeAsync(GameHub.ResolveSavedGame, new ResolveSavedGameRequest(saved.GameId, Resume: false), Ct);

        // Then
        Assert.Equal(GamePhase.Lobby, Game.State.Phase);
        Assert.NotEqual(saved.GameId, Game.State.GameId);
        Assert.Equal("soiree", Game.State.SelectedPackId);
        Assert.Equal(saved.GameId.Value, JsonNode.Parse(await File.ReadAllTextAsync(PreviousFile, Ct))!["game"]!["gameId"]!.GetValue<Guid>());
        await using var zoe = await HubClients.ConnectAsync(_factory!);
        var resumed = await zoe.InvokeAsync<ResumeSessionResult>(GameHub.ResumeSession, new ResumeSessionRequest(saved.Tokens[0]), Ct);
        Assert.Equal(ResumeSessionRefusal.SessionUnknown, resumed.Refusal);
        Assert.Null((await zoe.InvokeAsync<JoinResult>(GameHub.JoinGame, new JoinRequest("Zoé"), Ct)).Refusal);
        await EventuallyAsync(() => SavedGameId() == Game.State.GameId);
    }

    [Fact]
    public async Task ResolveSavedGame_MediaMissingThenBack_ResumesOnlyOnceCheckedAgain()
    {
        // Given: the image of the pack was moved while the server was down
        var saved = await SaveGameAsync(started: true, "Zoé");
        var moved = MediaFile + ".moved";
        File.Move(MediaFile, moved);
        Restart();
        await using var gameMaster = await ConnectGameMasterAsync();
        using var toGameMaster = new ReceivedSnapshots(gameMaster);
        await AnnounceAsync(gameMaster, Role.GameMaster, Code);
        await gameMaster.InvokeAsync(GameHub.ResolveSavedGame, new ResolveSavedGameRequest(saved.GameId, Resume: true), Ct);
        Assert.Equal(GamePhase.ResumePending, Game.State.Phase);

        // When
        File.Move(moved, MediaFile);
        await gameMaster.InvokeAsync(GameHub.CheckSavedGameMedia, Ct);
        await gameMaster.InvokeAsync(GameHub.ResolveSavedGame, new ResolveSavedGameRequest(saved.GameId, Resume: true), Ct);

        // Then
        Assert.Equal(GamePhase.Round, Game.State.Phase);
        await FlushAsync(gameMaster);
        Assert.Equal([Image], toGameMaster.GameMaster[0].SavedGame!.MissingMedia);
        Assert.Empty(toGameMaster.GameMaster[1].SavedGame!.MissingMedia);
    }

    [Fact]
    public async Task ResolveSavedGame_SecondDecisionOfAnotherConsole_IsIgnored()
    {
        // Given
        var saved = await SaveGameAsync(started: true, "Zoé");
        Restart();
        await using var gameMaster = await ConnectGameMasterAsync();
        await using var secondGameMaster = await ConnectGameMasterAsync();
        await AnnounceAsync(gameMaster, Role.GameMaster, Code);
        await AnnounceAsync(secondGameMaster, Role.GameMaster, Code);
        await gameMaster.InvokeAsync(GameHub.ResolveSavedGame, new ResolveSavedGameRequest(saved.GameId, Resume: true), Ct);
        var state = Game.State;

        // When
        await secondGameMaster.InvokeAsync(GameHub.ResolveSavedGame, new ResolveSavedGameRequest(saved.GameId, Resume: false), Ct);

        // Then
        Assert.Same(state, Game.State);
        Assert.False(File.Exists(PreviousFile));
    }

    [Fact]
    public async Task Pending_PhonesAndPreviousCode_AreKeptWaiting()
    {
        // Given
        await SaveGameAsync(started: false, "Zoé");

        // When
        Restart();
        var welcome = new TaskCompletionSource<Welcome>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var newcomer = await HubClients.ConnectAsync(_factory!, beforeStart: c => c.On<Welcome>(nameof(IGameClient.ReceiveWelcome), w => welcome.TrySetResult(w)));
        var joined = await newcomer.InvokeAsync<JoinResult>(GameHub.JoinGame, new JoinRequest("Léa"), Ct);
        await using var gameMaster = await ConnectGameMasterAsync();
        var announced = await gameMaster.InvokeAsync<AnnouncementResult>(GameHub.Announce, new Announcement(Role.GameMaster, PreviousCode), Ct);

        // Then: a phone without token waits as well, and the console asks for the code of this startup
        Assert.True((await welcome.Task.WaitAsync(_timeout, Ct)).GamePending);
        Assert.Equal(JoinRefusal.GamePending, joined.Refusal);
        Assert.Equal(AnnouncementRefusal.GameMasterCodeInvalid, announced.Refusal);
        Assert.Empty(Game.State.Players);
    }

    /// <summary>
    /// Plays a first run of the server on the data folder: the players join, then the game master starts the game if asked,
    /// and the server stops, saving the game.
    /// </summary>
    private async Task<(GameId GameId, long Version, string[] Tokens)> SaveGameAsync(bool started, params string[] nicknames)
    {
        var factory = CreateFactory(PreviousCode);
        var tokens = new List<string>();
        foreach (var nickname in nicknames)
        {
            await using var phone = await HubClients.ConnectAsync(factory);
            tokens.Add((await phone.InvokeAsync<JoinResult>(GameHub.JoinGame, new JoinRequest(nickname), Ct)).Token!);
        }

        if (started)
        {
            await using var gameMaster = await HubClients.ConnectAsync(factory);
            await AnnounceAsync(gameMaster, Role.GameMaster, PreviousCode);
            Assert.Null((await gameMaster.StartGameAndFirstRoundAsync(factory.Services.GetRequiredService<GameLoop>(), Ct)).Refusal);
        }

        var gameId = factory.Services.GetRequiredService<GameLoop>().State.GameId;
        await factory.DisposeAsync();
        return (gameId, SavedVersion()!.Value, [.. tokens]);
    }

    private void Restart() => _ = (_factory = CreateFactory(Code)).Services;

    private WebApplicationFactory<Program> CreateFactory(string code) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder
            .UseScratchDirectory(_data.Path)
            .UseSetting("GameMaster:Code", code)
            .UseSetting(PacksOptions.DirectorySetting, _packs.Path));

    private Task<HubConnection> ConnectGameMasterAsync() => HubClients.ConnectAsync(_factory!);

    private static async Task AnnounceAsync(HubConnection connection, Role role, string? code) =>
        Assert.Null((await connection.InvokeAsync<AnnouncementResult>(GameHub.Announce, new Announcement(role, code), Ct)).Refusal);

    private async Task FlushAsync(params HubConnection[] connections)
    {
        foreach (var connection in connections)
        {
            await HubClients.FlushAsync<GameHub>(_factory!, connection);
        }
    }

    private long? SavedVersion() => ReadSave()?["game"]?["version"]?.GetValue<long>();

    private GameId? SavedGameId() => ReadSave()?["game"]?["gameId"]?.GetValue<Guid>() is { } id ? new GameId(id) : null;

    private JsonNode? ReadSave()
    {
        try
        {
            return File.Exists(SaveFile) ? JsonNode.Parse(File.ReadAllText(SaveFile)) : null;
        }
        catch (IOException)
        {
            return null; // replaced at this very moment
        }
    }

    private static async Task EventuallyAsync(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow + _timeout;
        while (!condition())
        {
            Assert.True(DateTime.UtcNow < deadline, "The condition was not met in time.");
            await Task.Delay(20, Ct);
        }
    }
}
