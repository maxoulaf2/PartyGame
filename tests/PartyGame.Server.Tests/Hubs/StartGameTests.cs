using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using PartyGame.Contracts;
using PartyGame.Engine;
using PartyGame.Server.Games;
using PartyGame.Server.Hubs;
using PartyGame.Server.Packs;
using PartyGame.Server.Tests.Packs;

namespace PartyGame.Server.Tests.Hubs;

public sealed class StartGameTests : IAsyncDisposable
{
    private const string Code = "482913";

    private readonly TempDirectory _logs = new();
    private readonly TempDirectory _packs = new();
    private readonly WebApplicationFactory<Program> _factory;

    public StartGameTests()
    {
        // The only pack of the directory, chosen at once, so that the game can start.
        TestPacks.Write(_packs.Path, "soiree", TestPacks.Quiz("Soirée test", "Manche"));
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
    public async Task StartGame_AsGameMasterWithPlayers_ShowsTheStartedGameOnEveryInterface()
    {
        // Given
        await using var display = await HubClients.ConnectAsync(_factory);
        await using var gameMaster = await ConnectGameMasterAsync();
        await using var zoe = await HubClients.ConnectAsync(_factory);
        using var toDisplay = new ReceivedSnapshots(display);
        using var toGameMaster = new ReceivedSnapshots(gameMaster);
        using var toZoe = new ReceivedSnapshots(zoe);
        await AnnounceAsync(display, Role.Display);
        await JoinAsync(zoe, "Zoé");

        // When
        var result = await StartAsync(gameMaster);

        // Then
        Assert.Equal(new StartGameResult(Refusal: null), result);
        // The quiz mode does not play its questions yet (E08): the single round of the pack finishes as soon as it starts.
        Assert.Equal(GamePhase.Finished, Game.State.Phase);
        await Task.WhenAll(FlushAsync(display), FlushAsync(gameMaster), FlushAsync(zoe));
        Assert.Equal(Phase.Finished, toDisplay.Display[^1].Phase);
        Assert.Equal(Phase.Finished, toGameMaster.GameMaster[^1].Phase);
        Assert.Equal(Phase.Finished, toZoe.Player[^1].Phase);
        Assert.Contains(LoggedEvent.ReadAll(_logs), e => e.Template.StartsWith("Game started", StringComparison.Ordinal));
    }

    [Fact]
    public async Task StartGame_WithoutPlayers_IsRefusedAndBroadcastsNothing()
    {
        // Given
        await using var gameMaster = await ConnectGameMasterAsync();
        await FlushAsync(gameMaster);
        using var toGameMaster = new ReceivedSnapshots(gameMaster);
        var state = Game.State;

        // When
        var result = await StartAsync(gameMaster);

        // Then
        Assert.Equal(new StartGameResult(StartGameRefusal.NotEnoughPlayers), result);
        Assert.Same(state, Game.State);
        await FlushAsync(gameMaster);
        Assert.Empty(toGameMaster.Json);
    }

    [Fact]
    public async Task StartGame_AlreadyStarted_IsRefusedAndBroadcastsNothing()
    {
        // Given
        await using var gameMaster = await ConnectGameMasterAsync();
        await using var secondGameMaster = await ConnectGameMasterAsync();
        await using var zoe = await HubClients.ConnectAsync(_factory);
        await JoinAsync(zoe, "Zoé");
        Assert.Null((await StartAsync(gameMaster))?.Refusal);
        await FlushAsync(secondGameMaster);
        using var toGameMaster = new ReceivedSnapshots(secondGameMaster);
        var state = Game.State;

        // When
        var result = await StartAsync(secondGameMaster);

        // Then
        Assert.Equal(new StartGameResult(StartGameRefusal.AlreadyStarted), result);
        Assert.Same(state, Game.State);
        await FlushAsync(secondGameMaster);
        Assert.Empty(toGameMaster.Json);
    }

    [Fact]
    public async Task StartGame_TwoAtOnce_StartsTheGameOnce()
    {
        // Given
        await using var gameMaster = await ConnectGameMasterAsync();
        await using var zoe = await HubClients.ConnectAsync(_factory);
        await JoinAsync(zoe, "Zoé");
        var version = Game.State.Version;

        // When
        var results = await Task.WhenAll(StartAsync(gameMaster), StartAsync(gameMaster));

        // Then
        Assert.Equal(
            [null, StartGameRefusal.AlreadyStarted],
            results.Select(r => r?.Refusal).Order());
        Assert.Equal(version + 1, Game.State.Version);
    }

    [Fact]
    public async Task JoinGame_AfterStart_RegistersThePlayerWhoSeesTheStartedGame()
    {
        // Given
        await using var gameMaster = await ConnectGameMasterAsync();
        await using var display = await HubClients.ConnectAsync(_factory);
        await using var zoe = await HubClients.ConnectAsync(_factory);
        await using var max = await HubClients.ConnectAsync(_factory);
        await AnnounceAsync(display, Role.Display);
        await JoinAsync(zoe, "Zoé");
        Assert.Null((await StartAsync(gameMaster))?.Refusal);
        using var toDisplay = new ReceivedSnapshots(display);
        using var toGameMaster = new ReceivedSnapshots(gameMaster);
        using var toMax = new ReceivedSnapshots(max);

        // When
        await JoinAsync(max, "Max");

        // Then
        await Task.WhenAll(FlushAsync(display), FlushAsync(gameMaster), FlushAsync(max));
        Assert.Equal((Phase.Finished, "Max"), (toMax.Player[^1].Phase, toMax.Player[^1].Nickname));
        Assert.Equal(["Zoé", "Max"], toDisplay.Display[^1].Players.Select(p => p.Nickname));
        Assert.Equal(["Zoé", "Max"], toGameMaster.GameMaster[^1].Players.Select(p => p.Nickname));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(Role.Display)]
    public async Task StartGame_NotAuthenticatedAsGameMaster_IsIgnored(Role? role)
    {
        // Given
        await using var connection = await HubClients.ConnectAsync(_factory);
        await using var zoe = await HubClients.ConnectAsync(_factory);
        if (role is { } announced)
        {
            await AnnounceAsync(connection, announced);
        }

        await JoinAsync(zoe, "Zoé");
        var state = Game.State;

        // When
        var result = await StartAsync(connection);

        // Then
        Assert.Null(result);
        Assert.Same(state, Game.State);
        Assert.Contains(LoggedEvent.ReadAll(_logs), e => e.Template.Contains("not authenticated as game master", StringComparison.Ordinal));
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

    /// <summary>
    /// Starts the game through the hub. The answer is <see langword="null"/> when the hub ignores the intent of a
    /// connection that is not authenticated as game master.
    /// </summary>
    private static Task<StartGameResult?> StartAsync(HubConnection connection) =>
        connection.InvokeAsync<StartGameResult?>(GameHub.StartGame, Ct);

    private Task FlushAsync(HubConnection connection) => HubClients.FlushAsync<GameHub>(_factory, connection);
}
