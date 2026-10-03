using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using PartyGame.Contracts;
using PartyGame.Engine;
using PartyGame.Server.Games;
using PartyGame.Server.Hubs;
using PartyGame.Server.Packs;
using PartyGame.Server.Tests.Packs;
using PartyGame.Tests.Shared.Leaks;

namespace PartyGame.Server.Tests.Hubs;

public sealed class PackChoiceTests : IAsyncDisposable
{
    private const string Code = "482913";

    private readonly TempDirectory _logs = new();
    private readonly TempDirectory _packs = new();
    private readonly WebApplicationFactory<Program> _factory;

    public PackChoiceTests()
    {
        // Two valid packs, so that none is chosen at startup, and an invalid one.
        TestPacks.Write(_packs.Path, "apero", TestPacks.Quiz("Quiz de l'apéro", "Mise en bouche"));
        TestPacks.Write(_packs.Path, "casse", TestPacks.Broken("Pack cassé"));
        TestPacks.Write(_packs.Path, "soiree", TestPacks.Quiz("Grande soirée", "Échauffement", "Finale"));
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder
            .UseSetting("LogFiles:Directory", _logs.Path)
            .UseSetting("GameMaster:Code", Code)
            .UseSetting(PacksOptions.DirectorySetting, _packs.Path));
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private GameLoop Game => _factory.Services.GetRequiredService<GameLoop>();

    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
        _logs.Dispose();
        _packs.Dispose();
    }

    [Fact]
    public async Task Announce_GameMaster_ShowsThePacksOfTheDirectoryWithTheProblemsOfTheInvalidOne()
    {
        // Given
        await using var gameMaster = await HubClients.ConnectAsync(_factory);
        using var toGameMaster = new ReceivedSnapshots(gameMaster);

        // When
        await AnnounceAsync(gameMaster, Role.GameMaster, Code);

        // Then
        await FlushAsync(gameMaster);
        var snapshot = toGameMaster.GameMaster[^1];
        var catalog = Assert.IsType<GameMasterPackCatalog>(snapshot.PackCatalog);
        Assert.Equal(Path.GetFullPath(_packs.Path), catalog.Directory);
        Assert.Equal(["apero", "casse", "soiree"], catalog.Packs.Select(p => p.Id));
        Assert.Equal(
            [new GameMasterPackRound("Échauffement", "quiz"), new GameMasterPackRound("Finale", "quiz")],
            catalog.Packs[2].Rounds);
        var broken = catalog.Packs[1];
        Assert.False(broken.IsValid);
        var problem = Assert.Single(broken.Problems);
        Assert.Equal((PackProblemCode.QuizCorrectChoiceMissing, "pack.json", "$.rounds[0].questions[0]"), (problem.Code, problem.File, problem.Path));
        Assert.Null(snapshot.SelectedPackId);
    }

    [Fact]
    public async Task SelectPack_ValidPack_IsShownOnEveryConsoleAndItsTitleOnTheDisplay()
    {
        // Given
        await using var gameMaster = await ConnectGameMasterAsync();
        await using var secondGameMaster = await ConnectGameMasterAsync();
        await using var display = await HubClients.ConnectAsync(_factory);
        await using var zoe = await HubClients.ConnectAsync(_factory);
        await AnnounceAsync(display, Role.Display);
        await JoinAsync(zoe, "Zoé");
        using var toSecondGameMaster = new ReceivedSnapshots(secondGameMaster);
        using var toDisplay = new ReceivedSnapshots(display);
        using var toZoe = new ReceivedSnapshots(zoe);

        // When
        var result = await SelectAsync(gameMaster, "soiree");

        // Then
        Assert.Equal(new SelectPackResult(Refusal: null), result);
        Assert.Equal("soiree", Game.State.SelectedPackId);
        await Task.WhenAll(FlushAsync(secondGameMaster), FlushAsync(display), FlushAsync(zoe));
        Assert.Equal(("soiree", "Grande soirée"), (toSecondGameMaster.GameMaster[^1].SelectedPackId, toSecondGameMaster.GameMaster[^1].PackTitle));
        Assert.Equal("Grande soirée", toDisplay.Display[^1].PackTitle);
        Secret[] secrets = [.. new[] { "apero", "Échauffement", nameof(PackProblemCode.QuizCorrectChoiceMissing) }.Select(s => new Secret(s, Audience.AllButGameMaster))];
        LeakAssert.NoSecretReceived(Viewer.Display, toDisplay.Json, secrets);
        LeakAssert.NoSecretReceived(Viewer.PhoneOf("Zoé"), toZoe.Json, secrets);
        Assert.Contains(LoggedEvent.ReadAll(_logs), e => e.Template.StartsWith("Pack {PackId} chosen", StringComparison.Ordinal));
    }

    [Fact]
    public async Task SelectPack_TheSelectedPackAgain_IsAcceptedAndBroadcastsNothing()
    {
        // Given: a double tap, or a second game master
        await using var gameMaster = await ConnectGameMasterAsync();
        await SelectAsync(gameMaster, "soiree");
        await FlushAsync(gameMaster);
        using var toGameMaster = new ReceivedSnapshots(gameMaster);
        var state = Game.State;

        // When
        var result = await SelectAsync(gameMaster, "soiree");

        // Then
        Assert.Equal(new SelectPackResult(Refusal: null), result);
        Assert.Same(state, Game.State);
        await FlushAsync(gameMaster);
        Assert.Empty(toGameMaster.Json);
    }

    [Theory]
    [InlineData("inconnu", SelectPackRefusal.PackUnknown)]
    [InlineData("casse", SelectPackRefusal.PackInvalid)]
    public async Task SelectPack_PackThatCannotBeChosen_IsRefused(string packId, SelectPackRefusal refusal)
    {
        // Given
        await using var gameMaster = await ConnectGameMasterAsync();
        var state = Game.State;

        // When
        var result = await SelectAsync(gameMaster, packId);

        // Then
        Assert.Equal(new SelectPackResult(refusal), result);
        Assert.Same(state, Game.State);
    }

    [Fact]
    public async Task SelectPack_MalformedMessage_IsRefusedAndLogged()
    {
        // Given
        await using var gameMaster = await ConnectGameMasterAsync();

        // When
        var result = await gameMaster.InvokeAsync<SelectPackResult>(GameHub.SelectPack, new { packId = 42 }, Ct);

        // Then
        Assert.Equal(new SelectPackResult(SelectPackRefusal.MessageInvalid), result);
        Assert.Contains(LoggedEvent.ReadAll(_logs), e => e.Template.StartsWith("Malformed {HubMethod}", StringComparison.Ordinal));
    }

    [Fact]
    public async Task SelectPack_NotAuthenticatedAsGameMaster_IsIgnored()
    {
        // Given
        await using var display = await HubClients.ConnectAsync(_factory);
        await AnnounceAsync(display, Role.Display);
        var state = Game.State;

        // When
        var result = await display.InvokeAsync<SelectPackResult?>(GameHub.SelectPack, new SelectPackRequest("soiree"), Ct);

        // Then
        Assert.Null(result);
        Assert.Same(state, Game.State);
    }

    [Fact]
    public async Task StartGame_WithoutSelectedPack_IsRefused()
    {
        // Given
        await using var gameMaster = await ConnectGameMasterAsync();
        await using var zoe = await HubClients.ConnectAsync(_factory);
        await JoinAsync(zoe, "Zoé");
        var state = Game.State;

        // When
        var result = await gameMaster.InvokeAsync<StartGameResult>(GameHub.StartGame, Ct);

        // Then
        Assert.Equal(new StartGameResult(StartGameRefusal.PackNotSelected), result);
        Assert.Same(state, Game.State);
    }

    [Fact]
    public async Task StartGame_WithSelectedPack_PlaysThatPack()
    {
        // Given
        await using var gameMaster = await ConnectGameMasterAsync();
        await using var display = await HubClients.ConnectAsync(_factory);
        await using var zoe = await HubClients.ConnectAsync(_factory);
        await AnnounceAsync(display, Role.Display);
        await JoinAsync(zoe, "Zoé");
        await SelectAsync(gameMaster, "soiree");
        using var toGameMaster = new ReceivedSnapshots(gameMaster);
        using var toDisplay = new ReceivedSnapshots(display);

        // When
        var result = await gameMaster.InvokeAsync<StartGameResult>(GameHub.StartGame, Ct);

        // Then
        Assert.Null(result.Refusal);
        Assert.Equal(["Échauffement", "Finale"], Game.State.Rounds.Select(r => r.Title));
        await Task.WhenAll(FlushAsync(gameMaster), FlushAsync(display));
        var started = toGameMaster.GameMaster[^1];
        Assert.Equal((null, "soiree", "Grande soirée"), (started.PackCatalog, started.SelectedPackId, started.PackTitle));
        Assert.Equal("Grande soirée", toDisplay.Display[^1].PackTitle);
        Assert.Contains(LoggedEvent.ReadAll(_logs), e => e.Template.StartsWith("Game started", StringComparison.Ordinal) && e.Template.Contains("{PackId}", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ReloadPacks_AfterFixingAndRemovingPacks_ShowsTheNewCatalogOnEveryConsole()
    {
        // Given
        await using var gameMaster = await ConnectGameMasterAsync();
        await using var secondGameMaster = await ConnectGameMasterAsync();
        using var toSecondGameMaster = new ReceivedSnapshots(secondGameMaster);
        TestPacks.Write(_packs.Path, "casse", TestPacks.Quiz("Pack réparé", "Manche"));
        Directory.Delete(Path.Combine(_packs.Path, "apero"), recursive: true);

        // When
        var result = await ReloadAsync(gameMaster);

        // Then
        Assert.Equal(new ReloadPacksResult(Refusal: null), result);
        await FlushAsync(secondGameMaster);
        var catalog = toSecondGameMaster.GameMaster[^1].PackCatalog!;
        Assert.Equal([("casse", "Pack réparé", true), ("soiree", "Grande soirée", true)], catalog.Packs.Select(p => (p.Id, p.Title, p.IsValid)));
        Assert.Contains(LoggedEvent.ReadAll(_logs), e => e.Template.StartsWith("Packs reloaded", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ReloadPacks_SelectedPackBrokenOnTheDisk_CancelsTheSelection()
    {
        // Given
        await using var gameMaster = await ConnectGameMasterAsync();
        await using var display = await HubClients.ConnectAsync(_factory);
        await AnnounceAsync(display, Role.Display);
        await SelectAsync(gameMaster, "soiree");
        using var toDisplay = new ReceivedSnapshots(display);
        TestPacks.Write(_packs.Path, "soiree", TestPacks.Broken("Grande soirée"));

        // When
        await ReloadAsync(gameMaster);

        // Then
        Assert.Null(Game.State.SelectedPackId);
        await FlushAsync(display);
        Assert.Null(toDisplay.Display[^1].PackTitle);
    }

    [Fact]
    public async Task ReloadPacks_OnceStarted_IsRefusedAndTheGameKeepsItsPack()
    {
        // Given
        await using var gameMaster = await ConnectGameMasterAsync();
        await using var zoe = await HubClients.ConnectAsync(_factory);
        await JoinAsync(zoe, "Zoé");
        await SelectAsync(gameMaster, "soiree");
        Assert.Null((await gameMaster.InvokeAsync<StartGameResult>(GameHub.StartGame, Ct)).Refusal);
        TestPacks.Write(_packs.Path, "soiree", TestPacks.Quiz("Autre titre", "Autre manche"));
        var state = Game.State;

        // When
        var result = await ReloadAsync(gameMaster);

        // Then
        Assert.Equal(new ReloadPacksResult(ReloadPacksRefusal.AlreadyStarted), result);
        Assert.Same(state, Game.State);
        Assert.Equal("Grande soirée", Game.State.Pack!.Title);
    }

    [Fact]
    public async Task SelectPack_OnceStarted_IsRefused()
    {
        // Given
        await using var gameMaster = await ConnectGameMasterAsync();
        await using var zoe = await HubClients.ConnectAsync(_factory);
        await JoinAsync(zoe, "Zoé");
        await SelectAsync(gameMaster, "soiree");
        await gameMaster.InvokeAsync<StartGameResult>(GameHub.StartGame, Ct);

        // When
        var result = await SelectAsync(gameMaster, "apero");

        // Then
        Assert.Equal(new SelectPackResult(SelectPackRefusal.AlreadyStarted), result);
        Assert.Equal("soiree", Game.State.SelectedPackId);
        Assert.Equal(GamePhase.BetweenRounds, Game.State.Phase);
    }

    [Fact]
    public async Task ReloadPacks_NotAuthenticatedAsGameMaster_IsIgnored()
    {
        // Given
        await using var connection = await HubClients.ConnectAsync(_factory);
        var state = Game.State;

        // When
        var result = await connection.InvokeAsync<ReloadPacksResult?>(GameHub.ReloadPacks, Ct);

        // Then
        Assert.Null(result);
        Assert.Same(state, Game.State);
    }

    private async Task<HubConnection> ConnectGameMasterAsync()
    {
        var connection = await HubClients.ConnectAsync(_factory);
        await AnnounceAsync(connection, Role.GameMaster, Code);
        return connection;
    }

    private static async Task AnnounceAsync(HubConnection connection, Role role, string? code = null)
    {
        var result = await connection.InvokeAsync<AnnouncementResult>(GameHub.Announce, new Announcement(role, code), Ct);
        Assert.Null(result.Refusal);
    }

    private static async Task JoinAsync(HubConnection connection, string nickname)
    {
        var result = await connection.InvokeAsync<JoinResult>(GameHub.JoinGame, new JoinRequest(nickname), Ct);
        Assert.Null(result.Refusal);
    }

    private static Task<SelectPackResult> SelectAsync(HubConnection connection, string packId) =>
        connection.InvokeAsync<SelectPackResult>(GameHub.SelectPack, new SelectPackRequest(packId), Ct);

    private static Task<ReloadPacksResult> ReloadAsync(HubConnection connection) =>
        connection.InvokeAsync<ReloadPacksResult>(GameHub.ReloadPacks, Ct);

    private Task FlushAsync(HubConnection connection) => HubClients.FlushAsync<GameHub>(_factory, connection);
}
