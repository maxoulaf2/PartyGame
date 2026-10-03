using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using PartyGame.Contracts;
using PartyGame.Engine;
using PartyGame.Engine.Inputs;
using PartyGame.Server.Games;
using PartyGame.Server.Hubs;
using PartyGame.Tests.Shared.Leaks;

namespace PartyGame.Server.Tests.Hubs;

public sealed class SnapshotBroadcastTests : IAsyncDisposable
{
    private const string Code = "482913";

    private readonly TempDirectory _logs = new();
    private readonly WebApplicationFactory<Program> _factory;

    public SnapshotBroadcastTests()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder
            .UseSetting("LogFiles:Directory", _logs.Path)
            .UseSetting("GameMaster:Code", Code));
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private GameId GameId => _factory.Services.GetRequiredService<GameLoop>().State.GameId;

    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
        _logs.Dispose();
    }

    [Fact]
    public async Task Announce_Display_ReceivesCurrentDisplaySnapshotAtOnce()
    {
        // Given
        await using var connection = await HubClients.ConnectAsync(_factory);
        using var received = new ReceivedSnapshots(connection);

        // When
        await AnnounceAsync(connection, Role.Display);

        // Then
        await FlushAsync(connection);
        var snapshot = Assert.Single(received.Display);
        Assert.Equal((GameId, 1, Phase.Lobby), (snapshot.GameId, snapshot.Version, snapshot.Phase));
        Assert.Empty(snapshot.Players);
        Assert.Single(received.Json);
    }

    [Fact]
    public async Task Announce_GameMaster_ReceivesCurrentGameMasterSnapshotAtOnce()
    {
        // Given
        await JoinAsync("Zoé", player: 1);
        await using var connection = await HubClients.ConnectAsync(_factory);
        using var received = new ReceivedSnapshots(connection);

        // When
        await AnnounceAsync(connection, Role.GameMaster, Code);

        // Then
        await FlushAsync(connection);
        var snapshot = Assert.Single(received.GameMaster);
        Assert.Equal((GameId, 2, Phase.Lobby), (snapshot.GameId, snapshot.Version, snapshot.Phase));
        Assert.Equal([new GameMasterPlayer(PlayerIdOf(1), "Zoé", IsConnected: true, Score: 0)], snapshot.Players);
        Assert.Single(received.Json);
    }

    [Fact]
    public async Task Announce_GameMasterWithWrongCode_ReceivesNoSnapshot()
    {
        // Given
        await using var connection = await HubClients.ConnectAsync(_factory);
        using var received = new ReceivedSnapshots(connection);

        // When
        await AnnounceAsync(connection, Role.GameMaster, "135792");
        await JoinAsync("Zoé", player: 1);

        // Then
        await FlushAsync(connection);
        Assert.Empty(received.Json);
    }

    [Fact]
    public async Task StateChange_ConnectedClients_EachReceiveTheNewVersionOfTheirRole()
    {
        // Given
        await using var display = await HubClients.ConnectAsync(_factory);
        await using var gameMaster = await HubClients.ConnectAsync(_factory);
        await using var zoe = await HubClients.ConnectAsync(_factory);
        await using var unidentified = await HubClients.ConnectAsync(_factory);
        using var toDisplay = new ReceivedSnapshots(display);
        using var toGameMaster = new ReceivedSnapshots(gameMaster);
        using var toZoe = new ReceivedSnapshots(zoe);
        using var toUnidentified = new ReceivedSnapshots(unidentified);
        await AnnounceAsync(display, Role.Display);
        await AnnounceAsync(gameMaster, Role.GameMaster, Code);
        var zoeId = await JoinAsync("Zoé", player: 1);
        await AddToPlayerGroupAsync(zoe, zoeId);

        // When
        await JoinAsync("Max", player: 2);

        // Then
        await Task.WhenAll(FlushAsync(display), FlushAsync(gameMaster), FlushAsync(zoe), FlushAsync(unidentified));
        Assert.Equal([1, 2, 3], toDisplay.Display.Select(s => s.Version));
        Assert.Equal(
            [[], ["Zoé"], ["Zoé", "Max"]],
            toDisplay.Display.Select(s => s.Players.Select(p => p.Nickname).ToArray()));
        Assert.Equal([1, 2, 3], toGameMaster.GameMaster.Select(s => s.Version));
        Assert.Equal([new PlayerSnapshot(GameId, 3, Phase.Lobby, zoeId, "Zoé", Score: 0, PlayerCount: 2, Round: null, RoundView: null, Standing: null)], toZoe.Player);

        // Each connection gets the projection of its role only.
        Assert.Equal(toDisplay.Display.Count, toDisplay.Json.Count);
        Assert.Equal(toGameMaster.GameMaster.Count, toGameMaster.Json.Count);
        Assert.Equal(toZoe.Player.Count, toZoe.Json.Count);
        Assert.Empty(toUnidentified.Json);
    }

    [Fact]
    public async Task ConnectionLost_DisplayAnnounced_ReceivesThePlayerAsDisconnected()
    {
        // Given
        await using var display = await HubClients.ConnectAsync(_factory);
        using var toDisplay = new ReceivedSnapshots(display);
        await AnnounceAsync(display, Role.Display);
        await JoinAsync("Zoé", player: 1);
        await JoinAsync("Max", player: 2);

        // When
        var outcome = await _factory.Services.GetRequiredService<IGameInputWriter>().SubmitAsync(new PlayerConnectionLost(PlayerIdOf(1)), Ct);

        // Then
        Assert.Equal(InputStatus.Accepted, outcome.Status);
        await FlushAsync(display);
        var latest = toDisplay.Display.MaxBy(s => s.Version)!;
        Assert.Equal([("Zoé", false), ("Max", true)], latest.Players.Select(p => (p.Nickname, p.IsConnected)));
    }

    [Fact]
    public async Task Snapshots_EveryRole_ContainNeitherPlayerTokenNorGameMasterCode()
    {
        // Given
        await using var display = await HubClients.ConnectAsync(_factory);
        await using var gameMaster = await HubClients.ConnectAsync(_factory);
        await using var player = await HubClients.ConnectAsync(_factory);
        using var toDisplay = new ReceivedSnapshots(display);
        using var toGameMaster = new ReceivedSnapshots(gameMaster);
        using var toPlayer = new ReceivedSnapshots(player);
        await AnnounceAsync(display, Role.Display);
        await AnnounceAsync(gameMaster, Role.GameMaster, Code);
        await AddToPlayerGroupAsync(player, PlayerIdOf(1));

        // When
        await JoinAsync("Zoé", player: 1);
        await JoinAsync("Max", player: 2);

        // Then
        await Task.WhenAll(FlushAsync(display), FlushAsync(gameMaster), FlushAsync(player));
        Assert.Equal(3 + 3 + 2, toDisplay.Json.Count + toGameMaster.Json.Count + toPlayer.Json.Count);
        Secret[] secrets = [new(Code, Audience.Everyone), new(TokenOf(1).Value, Audience.Everyone), new(TokenOf(2).Value, Audience.Everyone)];
        LeakAssert.NoSecretReceived(Viewer.Display, toDisplay.Json, secrets);
        LeakAssert.NoSecretReceived(Viewer.GameMaster, toGameMaster.Json, secrets);
        LeakAssert.NoSecretReceived(Viewer.PhoneOf("Zoé"), toPlayer.Json, secrets);
    }

    private static PlayerId PlayerIdOf(int player) => new(new Guid(player, 0, 0, new byte[8]));

    // Long and distinctive, so that it cannot appear in a snapshot by chance.
    private static PlayerToken TokenOf(int player) => new($"secret-token-{player}-0f1e2d3c4b5a");

    private static Task<AnnouncementResult> AnnounceAsync(HubConnection connection, Role role, string? code = null) =>
        connection.InvokeAsync<AnnouncementResult>(GameHub.Announce, new Announcement(role, code), Ct);

    /// <summary>
    /// Registers a player through the queue, as the hub does, with a known token. Returns once the loop has broadcast the change.
    /// </summary>
    private async Task<PlayerId> JoinAsync(string nickname, int player)
    {
        var inputs = _factory.Services.GetRequiredService<IGameInputWriter>();
        var outcome = await inputs.SubmitAsync(new JoinGame(PlayerIdOf(player), TokenOf(player), nickname, DateTimeOffset.UnixEpoch), Ct);
        Assert.Equal(InputStatus.Accepted, outcome.Status);
        return PlayerIdOf(player);
    }

    /// <summary>
    /// Puts a connection in the group of a player, as the hub does once the player registers.
    /// </summary>
    private Task AddToPlayerGroupAsync(HubConnection connection, PlayerId playerId) =>
        _factory.Services.GetRequiredService<IHubContext<GameHub, IGameClient>>()
            .Groups.AddToGroupAsync(connection.ConnectionId!, HubGroups.Player(playerId), Ct);

    private Task FlushAsync(HubConnection connection) => HubClients.FlushAsync<GameHub>(_factory, connection);
}
