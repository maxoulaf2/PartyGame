using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using PartyGame.Contracts;
using PartyGame.Server.Games;
using PartyGame.Server.Hubs;

namespace PartyGame.Server.Tests.Hubs;

public sealed class ReportClientErrorTests : IAsyncDisposable
{
    private const string ReportedTemplate =
        "Client error {ErrorKind} on {Role} page {Page} from connection {ConnectionId}, player {PlayerId}: {ErrorMessage} (round view {RoundViewType}, snapshot {SnapshotVersion}, build {ClientBuildId}) {ErrorStack}";

    private const string DroppedTemplate =
        "Connection {ConnectionId} reports more than {ReportsPerWindow} client errors a minute: the extra ones are ignored";

    private static readonly DateTimeOffset _start = new(2026, 10, 3, 22, 0, 0, TimeSpan.Zero);

    private readonly TempDirectory _logs = new();
    private readonly FakeTimeProvider _time = new(_start);
    private readonly WebApplicationFactory<Program> _factory;

    public ReportClientErrorTests()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder
            .UseScratchDirectory(_logs.Path)
            .ConfigureTestServices(services => services.AddSingleton<TimeProvider>(_time)));
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
        _logs.Dispose();
    }

    [Fact]
    public async Task ReportClientError_AnonymousConnection_LogsWarningWithEveryField()
    {
        // Given
        await using var connection = await HubClients.ConnectAsync(_factory);
        var report = new ClientErrorReport(
            Role.Display,
            "/display/",
            ClientErrorKind.RenderFailed,
            "Cannot read properties of null",
            "TypeError: Cannot read properties of null\n    at QuizDisplay",
            "quiz",
            SnapshotVersion: 42,
            BuildId: "b1");

        // When
        await connection.InvokeAsync(GameHub.ReportClientError, report, Ct);

        // Then
        var warning = Assert.Single(LoggedEvent.ReadAll(_logs), e => e.Template == ReportedTemplate);
        Assert.Equal("Warning", warning.Level);
        using var json = JsonDocument.Parse(warning.Line);
        var properties = json.RootElement;
        Assert.Equal("RenderFailed", properties.GetProperty("ErrorKind").GetString());
        Assert.Equal("Display", properties.GetProperty("Role").GetString());
        Assert.Equal("/display/", properties.GetProperty("Page").GetString());
        Assert.Equal(connection.ConnectionId, properties.GetProperty("ConnectionId").GetString());
        Assert.Equal(JsonValueKind.Null, properties.GetProperty("PlayerId").ValueKind);
        Assert.Equal(report.Message, properties.GetProperty("ErrorMessage").GetString());
        Assert.Equal(report.Stack, properties.GetProperty("ErrorStack").GetString());
        Assert.Equal("quiz", properties.GetProperty("RoundViewType").GetString());
        Assert.Equal(42, properties.GetProperty("SnapshotVersion").GetInt64());
        Assert.Equal("b1", properties.GetProperty("ClientBuildId").GetString());
    }

    [Fact]
    public async Task ReportClientError_IdentifiedPlayer_LogsThePlayerTheServerKnows()
    {
        // Given
        await using var connection = await HubClients.ConnectAsync(_factory);
        var joined = await connection.InvokeAsync<JoinResult>(GameHub.JoinGame, new JoinRequest("Zoé"), Ct);

        // When
        await connection.InvokeAsync(GameHub.ReportClientError, Report(ClientErrorKind.Error), Ct);

        // Then
        var warning = Assert.Single(LoggedEvent.ReadAll(_logs), e => e.Template == ReportedTemplate);
        using var json = JsonDocument.Parse(warning.Line);
        Assert.Equal(joined.PlayerId!.Value.Value, json.RootElement.GetProperty("PlayerId").GetGuid());
    }

    [Fact]
    public async Task ReportClientError_LongFields_LogsThemTruncated()
    {
        // Given
        await using var connection = await HubClients.ConnectAsync(_factory);
        var report = new ClientErrorReport(
            Role.Player,
            new string('p', 1000),
            ClientErrorKind.UnhandledRejection,
            new string('m', 5000),
            new string('s', 10_000),
            new string('t', 1000),
            SnapshotVersion: null,
            BuildId: new string('b', 1000));

        // When
        await connection.InvokeAsync(GameHub.ReportClientError, report, Ct);

        // Then
        var warning = Assert.Single(LoggedEvent.ReadAll(_logs), e => e.Template == ReportedTemplate);
        using var json = JsonDocument.Parse(warning.Line);
        var properties = json.RootElement;
        Assert.Equal(
            (ClientErrorFields.MaxPageLength, ClientErrorFields.MaxMessageLength, ClientErrorFields.MaxStackLength, ClientErrorFields.MaxRoundViewTypeLength, ClientErrorFields.MaxBuildIdLength),
            (
                properties.GetProperty("Page").GetString()!.Length,
                properties.GetProperty("ErrorMessage").GetString()!.Length,
                properties.GetProperty("ErrorStack").GetString()!.Length,
                properties.GetProperty("RoundViewType").GetString()!.Length,
                properties.GetProperty("ClientBuildId").GetString()!.Length));
    }

    [Fact]
    public async Task ReportClientError_Always_LeavesTheStateUnchanged()
    {
        // Given
        await using var connection = await HubClients.ConnectAsync(_factory);
        var game = _factory.Services.GetRequiredService<GameLoop>();
        var state = game.State;

        // When
        await connection.InvokeAsync(GameHub.ReportClientError, Report(ClientErrorKind.Error), Ct);

        // Then
        Assert.Same(state, game.State);
    }

    public static TheoryData<string, string> MalformedReports => new()
    {
        { "{}", "$" },
        { "null", "$" },
        { """{ "role": "Player", "page": "/", "kind": "Crash", "message": "x", "stack": null, "roundViewType": null, "snapshotVersion": null, "buildId": null }""", "$.kind" },
        { """{ "role": "Player", "page": "/", "kind": "Error", "message": null, "stack": null, "roundViewType": null, "snapshotVersion": null, "buildId": null }""", "$.message" },
        { """{ "role": "Player", "page": "/", "kind": "Error", "message": "x", "stack": null, "roundViewType": null, "snapshotVersion": "1", "buildId": null }""", "$.snapshotVersion" },
    };

    [Theory]
    [MemberData(nameof(MalformedReports))]
    public async Task ReportClientError_MalformedMessage_LogsMalformedWarningOnly(string message, string expectedPath)
    {
        // Given
        await using var connection = await HubClients.ConnectAsync(_factory);
        using var json = JsonDocument.Parse(message);

        // When
        await connection.InvokeAsync(GameHub.ReportClientError, json.RootElement, Ct);

        // Then
        var events = LoggedEvent.ReadAll(_logs);
        var warning = Assert.Single(events, e => e.Template.StartsWith("Malformed", StringComparison.Ordinal));
        Assert.Equal("Warning", warning.Level);
        Assert.Contains($"\"HubMethod\":\"{GameHub.ReportClientError}\"", warning.Line, StringComparison.Ordinal);
        Assert.Contains($"\"JsonPath\":\"{expectedPath}\"", warning.Line, StringComparison.Ordinal);
        Assert.DoesNotContain(events, e => e.Template == ReportedTemplate);
    }

    [Fact]
    public async Task ReportClientError_TooManyFromOneConnection_LogsTheAllowanceAndOneDropWarning()
    {
        // Given
        await using var flooding = await HubClients.ConnectAsync(_factory);

        // When
        await SendAsync(flooding, ClientErrorAllowance.ReportsPerWindow + 5);

        // Then
        var events = LoggedEvent.ReadAll(_logs);
        Assert.Equal(ClientErrorAllowance.ReportsPerWindow, events.Count(e => e.Template == ReportedTemplate));
        var dropped = Assert.Single(events, e => e.Template == DroppedTemplate);
        Assert.Equal("Warning", dropped.Level);
    }

    [Fact]
    public async Task ReportClientError_AnotherConnection_HasItsOwnAllowance()
    {
        // Given
        await using var flooding = await HubClients.ConnectAsync(_factory);
        await using var other = await HubClients.ConnectAsync(_factory);
        await SendAsync(flooding, ClientErrorAllowance.ReportsPerWindow + 1);

        // When
        await other.InvokeAsync(GameHub.ReportClientError, Report(ClientErrorKind.RenderFailed), Ct);

        // Then
        Assert.Single(LoggedEvent.ReadAll(_logs), IsReported(ClientErrorKind.RenderFailed));
    }

    [Fact]
    public async Task ReportClientError_AMinuteLater_LogsAgain()
    {
        // Given
        await using var flooding = await HubClients.ConnectAsync(_factory);
        await SendAsync(flooding, ClientErrorAllowance.ReportsPerWindow + 1);
        _time.Advance(ClientErrorAllowance.Window);

        // When
        await flooding.InvokeAsync(GameHub.ReportClientError, Report(ClientErrorKind.RenderFailed), Ct);

        // Then
        Assert.Single(LoggedEvent.ReadAll(_logs), IsReported(ClientErrorKind.RenderFailed));
    }

    private static ClientErrorReport Report(ClientErrorKind kind) =>
        new(Role.Player, "/", kind, "boom", Stack: null, RoundViewType: null, SnapshotVersion: null, BuildId: null);

    private static async Task SendAsync(HubConnection connection, int count)
    {
        for (var i = 0; i < count; i++)
        {
            await connection.InvokeAsync(GameHub.ReportClientError, Report(ClientErrorKind.Error), Ct);
        }
    }

    private static Predicate<LoggedEvent> IsReported(ClientErrorKind kind) =>
        e => e.Template == ReportedTemplate && e.Line.Contains($"\"ErrorKind\":\"{kind}\"", StringComparison.Ordinal);
}
