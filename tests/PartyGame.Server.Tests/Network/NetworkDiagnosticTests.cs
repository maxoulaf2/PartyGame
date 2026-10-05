using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using PartyGame.Contracts;
using PartyGame.Contracts.Serialization;
using PartyGame.Server.Hubs;
using PartyGame.Server.Network;
using PartyGame.Server.Tests.Hubs;
using PartyGame.Tests.Shared.Leaks;

namespace PartyGame.Server.Tests.Network;

public sealed class NetworkDiagnosticTests : IAsyncDisposable
{
    private const string Code = "482913";
    private const string IPhone = "Mozilla/5.0 (iPhone; CPU iPhone OS 17_5 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.5 Mobile/15E148 Safari/604.1";

    private static readonly DateTimeOffset _start = new(2026, 10, 4, 18, 0, 0, TimeSpan.Zero);

    private readonly TempDirectory _logs = new();
    private readonly FakeTimeProvider _time = new(_start);
    private readonly WebApplicationFactory<Program> _factory;

    public NetworkDiagnosticTests()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder
            .UseScratchDirectory(_logs.Path)
            .UseSetting("GameMaster:Code", Code)
            .ConfigureTestServices(services => services.AddSingleton<TimeProvider>(_time)));
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
        _logs.Dispose();
    }

    [Fact]
    public async Task CheckNetwork_AnonymousWebSocketConnection_TellsItsTransport()
    {
        // Given: the diagnostic page, which never registers
        await using var page = await HubClients.ConnectAsync(_factory);

        // When
        var result = await page.InvokeAsync<NetworkCheckResult>(GameHub.CheckNetwork, Ct);

        // Then
        Assert.Equal(ConnectionTransport.WebSockets, result.Transport);
    }

    [Fact]
    public async Task ReportNetworkDiagnostic_FromAnIPhone_IsListedOnTheConsoleWithItsDeviceAndTime()
    {
        // Given
        await using var page = await HubClients.ConnectAsync(_factory, userAgent: IPhone);

        // When
        await page.InvokeAsync(GameHub.ReportNetworkDiagnostic, Message(new NetworkDiagnosticReport(DiagnosticVerdict.Reserved, 64)), Ct);

        // Then
        var health = await ConsoleHealthAsync();
        Assert.Equal(
            [new NetworkDiagnostic(DeviceKind.IPhone, _start.ToUnixTimeMilliseconds(), DiagnosticVerdict.Reserved, 64)],
            health.Diagnostics);
        Assert.Contains(LoggedEvent.ReadAll(_logs), e => e.Level == "Information" && e.Template.StartsWith("Network diagnostic run", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("""{ "verdict": "Good", "roundTripMedian": -1 }""")]
    [InlineData("""{ "verdict": "Perfect", "roundTripMedian": 10 }""")]
    [InlineData("""{ "roundTripMedian": 10 }""")]
    public async Task ReportNetworkDiagnostic_Malformed_IsIgnoredWithAWarning(string json)
    {
        // Given
        await using var page = await HubClients.ConnectAsync(_factory);

        // When
        await page.InvokeAsync(GameHub.ReportNetworkDiagnostic, JsonDocument.Parse(json).RootElement, Ct);

        // Then
        Assert.Empty((await ConsoleHealthAsync()).Diagnostics);
        Assert.Contains(LoggedEvent.ReadAll(_logs), e => e.Level == "Warning" && e.Template.StartsWith("Malformed {HubMethod}", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ReportConnectionQuality_BeforeAndAfterIdentifying_ShowsThePlayersAndTheDisplayOnTheConsole()
    {
        // Given: a phone and a TV screen that synchronize their clock as soon as they connect
        await using var display = await HubClients.ConnectAsync(_factory);
        await using var zoe = await HubClients.ConnectAsync(_factory);
        await ReportRoundTripAsync(display, 3);
        await ReportRoundTripAsync(zoe, 41);

        // When: they identify, then the phone synchronizes again
        await AnnounceAsync(display, Role.Display);
        var zoeId = await JoinAsync(zoe, "Zoé");
        await ReportRoundTripAsync(zoe, 27);

        // Then
        Assert.Equal(
            [
                new ConnectionQuality(PlayerId: null, ConnectionTransport.WebSockets, 3, Reconnections: 0),
                new ConnectionQuality(zoeId, ConnectionTransport.WebSockets, 27, Reconnections: 0),
            ],
            (await ConsoleHealthAsync()).Connections);
    }

    [Fact]
    public async Task ResumeSession_AfterALostConnection_CountsAReconnection()
    {
        // Given
        var token = await JoinAndLeaveAsync("Zoé");

        // When
        await using var back = await HubClients.ConnectAsync(_factory);
        var resumed = await back.InvokeAsync<ResumeSessionResult>(GameHub.ResumeSession, new ResumeSessionRequest(token), Ct);

        // Then
        Assert.Null(resumed.Refusal);
        Assert.Equal(1, Assert.Single((await ConsoleHealthAsync()).Connections).Reconnections);
    }

    [Fact]
    public async Task ReportConnectionQuality_FromTheGameMaster_IsNotListed()
    {
        // Given
        await using var console = await ConnectGameMasterAsync();

        // When
        await ReportRoundTripAsync(console, 8);

        // Then
        Assert.Empty((await ConsoleHealthAsync()).Connections);
    }

    [Fact]
    public async Task ReportDisplayAudio_LockedThenUnlocked_TellsTheConsoleUntilItIsUnlocked()
    {
        // Given: a TV screen whose browser does not let it play sound yet
        await using var display = await HubClients.ConnectAsync(_factory);
        await AnnounceAsync(display, Role.Display);
        await ReportAudioAsync(display, unlocked: false);
        Assert.False(Assert.Single((await ConsoleHealthAsync()).Connections).AudioUnlocked);

        // When: someone clicks « Démarrer » on it
        await ReportAudioAsync(display, unlocked: true);

        // Then
        Assert.True(Assert.Single((await ConsoleHealthAsync()).Connections).AudioUnlocked);
    }

    [Fact]
    public async Task ReportDisplayAudio_FromAPlayer_IsIgnored()
    {
        // Given
        await using var display = await HubClients.ConnectAsync(_factory);
        await using var zoe = await HubClients.ConnectAsync(_factory);
        await AnnounceAsync(display, Role.Display);
        await JoinAsync(zoe, "Zoé");

        // When
        await ReportAudioAsync(zoe, unlocked: true);

        // Then
        Assert.All((await ConsoleHealthAsync()).Connections, c => Assert.Null(c.AudioUnlocked));
    }

    [Fact]
    public async Task NetworkHealth_EveryFewSeconds_ReachesTheGameMasterOnly()
    {
        // Given: a console, the TV screen, a phone, and a diagnostic run
        await using var console = await ConnectGameMasterAsync();
        await using var display = await HubClients.ConnectAsync(_factory);
        await using var zoe = await HubClients.ConnectAsync(_factory);
        await using var page = await HubClients.ConnectAsync(_factory, userAgent: IPhone);
        using var toConsole = new ReceivedSnapshots(console);
        using var toDisplay = new ReceivedSnapshots(display);
        using var toZoe = new ReceivedSnapshots(zoe);
        await AnnounceAsync(display, Role.Display);
        await JoinAsync(zoe, "Zoé");
        await ReportRoundTripAsync(zoe, 27);
        await page.InvokeAsync(GameHub.ReportNetworkDiagnostic, Message(new NetworkDiagnosticReport(DiagnosticVerdict.Good, 12)), Ct);

        // When
        _time.Advance(NetworkHealthBroadcaster.Period);

        // Then
        await WaitUntilAsync(() => toConsole.NetworkHealth.Count > 0);
        var health = toConsole.NetworkHealth[^1];
        Assert.Single(health.Diagnostics);
        Assert.Equal(2, health.Connections.Length);
        await Task.WhenAll(FlushAsync(display), FlushAsync(zoe));
        Assert.Empty(toDisplay.NetworkHealth);
        Assert.Empty(toZoe.NetworkHealth);
        Secret[] secrets =
        [
            new(nameof(NetworkHealth.Diagnostics), Audience.AllButGameMaster),
            new(nameof(ConnectionQuality.Reconnections), Audience.AllButGameMaster),
            new(nameof(ConnectionQuality.RoundTrip), Audience.AllButGameMaster),
            new(nameof(ConnectionQuality.AudioUnlocked), Audience.AllButGameMaster),
        ];
        LeakAssert.NoSecretReceived(Viewer.Display, toDisplay.Messages, secrets);
        LeakAssert.NoSecretReceived(Viewer.PhoneOf("Zoé"), toZoe.Messages, secrets);
    }

    [Fact]
    public async Task GetDownload_DiagnosticPage_ServesAboutAMegabyteNeverCached()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync(new Uri(NetworkDiagnosticExtensions.DownloadPath, UriKind.Relative), Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.NoStore);
        Assert.Equal(NetworkDiagnosticExtensions.DownloadBytes, (await response.Content.ReadAsByteArrayAsync(Ct)).Length);
    }

    private static JsonElement Message<T>(T message) => JsonSerializer.SerializeToElement(message, ContractJsonOptions.Default);

    private static Task ReportAudioAsync(HubConnection connection, bool unlocked) =>
        connection.InvokeAsync(GameHub.ReportDisplayAudio, Message(new DisplayAudioReport(unlocked)), Ct);

    private static Task ReportRoundTripAsync(HubConnection connection, int roundTrip) =>
        connection.InvokeAsync(GameHub.ReportConnectionQuality, Message(new ConnectionQualityReport(roundTrip)), Ct);

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(Ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));
        while (!condition())
        {
            await Task.Delay(10, timeout.Token);
        }
    }

    /// <summary>What a console that announces itself now receives.</summary>
    private async Task<NetworkHealth> ConsoleHealthAsync()
    {
        var received = new TaskCompletionSource<NetworkHealth>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var console = await HubClients.ConnectAsync(
            _factory,
            beforeStart: connection => connection.On<NetworkHealth>(nameof(IGameClient.ReceiveNetworkHealth), health => received.TrySetResult(health)));
        await AnnounceAsync(console, Role.GameMaster, Code);
        return await received.Task.WaitAsync(TimeSpan.FromSeconds(10), Ct);
    }

    private async Task<string> JoinAndLeaveAsync(string nickname)
    {
        await using var phone = await HubClients.ConnectAsync(_factory);
        var result = await phone.InvokeAsync<JoinResult>(GameHub.JoinGame, new JoinRequest(nickname), Ct);
        return result.Token!;
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

    private static async Task<PlayerId> JoinAsync(HubConnection connection, string nickname)
    {
        var result = await connection.InvokeAsync<JoinResult>(GameHub.JoinGame, new JoinRequest(nickname), Ct);
        Assert.Null(result.Refusal);
        return result.PlayerId!.Value;
    }

    private Task FlushAsync(HubConnection connection) => HubClients.FlushAsync<GameHub>(_factory, connection);
}
