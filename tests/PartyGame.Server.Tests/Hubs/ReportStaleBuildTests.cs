using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using PartyGame.Contracts;
using PartyGame.Server.Games;
using PartyGame.Server.Hubs;

namespace PartyGame.Server.Tests.Hubs;

public sealed class ReportStaleBuildTests : IAsyncDisposable
{
    private const string StaleTemplate =
        "Connection {ConnectionId} still runs client build {ClientBuildId} after reloading to get build {ServerBuildId}: a cache or a proxy keeps serving the old pages";

    private readonly TempDirectory _webRoot = new();
    private readonly TempDirectory _logs = new();
    private readonly WebApplicationFactory<Program> _factory;

    public ReportStaleBuildTests()
    {
        Directory.CreateDirectory(_webRoot.Path);
        File.WriteAllText(Path.Combine(_webRoot.Path, "index.html"), "player page");
        File.WriteAllText(Path.Combine(_webRoot.Path, "build.json"), """{ "buildId": "new" }""");
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder
            .UseSetting(WebHostDefaults.WebRootKey, _webRoot.Path)
            .UseSetting("LogFiles:Directory", _logs.Path));
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
        _webRoot.Dispose();
        _logs.Dispose();
    }

    [Fact]
    public async Task ReportStaleBuild_AnonymousConnection_LogsWarningWithBothBuilds()
    {
        // Given
        await using var connection = await HubClients.ConnectAsync(_factory);

        // When
        await connection.InvokeAsync(GameHub.ReportStaleBuild, new StaleBuildReport("old"), Ct);

        // Then
        var warning = Assert.Single(LoggedEvent.ReadAll(_logs), e => e.Template == StaleTemplate);
        Assert.Equal("Warning", warning.Level);
        Assert.Contains("\"ClientBuildId\":\"old\"", warning.Line, StringComparison.Ordinal);
        Assert.Contains("\"ServerBuildId\":\"new\"", warning.Line, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ReportStaleBuild_Always_LeavesTheStateUnchanged()
    {
        // Given
        await using var connection = await HubClients.ConnectAsync(_factory);
        var game = _factory.Services.GetRequiredService<GameLoop>();
        var state = game.State;

        // When
        await connection.InvokeAsync(GameHub.ReportStaleBuild, new StaleBuildReport("old"), Ct);

        // Then
        Assert.Same(state, game.State);
    }

    public static TheoryData<string, string> MalformedReports => new()
    {
        { "{}", "$" },
        { """{ "clientBuildId": 42 }""", "$.clientBuildId" },
        { """{ "clientBuildId": null }""", "$.clientBuildId" },
        { "null", "$" },
        { $$"""{ "clientBuildId": "{{new string('x', 65)}}" }""", "$.clientBuildId" },
    };

    [Theory]
    [MemberData(nameof(MalformedReports))]
    public async Task ReportStaleBuild_MalformedMessage_LogsMalformedWarningOnly(string message, string expectedPath)
    {
        // Given
        await using var connection = await HubClients.ConnectAsync(_factory);
        using var json = JsonDocument.Parse(message);

        // When
        await connection.InvokeAsync(GameHub.ReportStaleBuild, json.RootElement, Ct);

        // Then
        var events = LoggedEvent.ReadAll(_logs);
        var warning = Assert.Single(events, e => e.Template.StartsWith("Malformed", StringComparison.Ordinal));
        Assert.Equal("Warning", warning.Level);
        Assert.Contains($"\"JsonPath\":\"{expectedPath}\"", warning.Line, StringComparison.Ordinal);
        Assert.DoesNotContain(events, e => e.Template == StaleTemplate);
    }
}
