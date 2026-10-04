using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using PartyGame.Contracts;
using PartyGame.Server.Hubs;

namespace PartyGame.Server.Tests.Hubs;

/// <summary>
/// The filters apply to every hub method. A probe hub, mapped beside the real one with the server's own SignalR
/// configuration, checks them apart from any game rule; <see cref="RenamePlayerTests"/> covers a real game master intent.
/// </summary>
public sealed class HubFilterTests : IAsyncDisposable
{
    private const string ProbePath = "/hub/probe";

    private readonly TempDirectory _logs = new();
    private readonly WebApplicationFactory<Program> _factory;

    public HubFilterTests()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder
            .UseScratchDirectory(_logs.Path)
            .ConfigureTestServices(services => services.AddTransient<IStartupFilter, ProbeHubStartup>()));
    }

    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
        _logs.Dispose();
    }

    [Fact]
    public async Task Invoke_GameMasterIntentWithoutAuthentication_IsIgnoredAndLogsWarning()
    {
        await using var connection = await HubClients.ConnectAsync(_factory, ProbePath);

        var result = await connection.InvokeAsync<string?>(nameof(ProbeHub.Restart), TestContext.Current.CancellationToken);

        Assert.Null(result);
        Assert.Equal(0, ProbeHub.Restarts(connection.ConnectionId!));
        var warning = Assert.Single(LoggedEvent.ReadAll(_logs), e => e.Template.Contains("not authenticated as game master", StringComparison.Ordinal));
        Assert.Equal("Warning", warning.Level);
        Assert.Contains("\"HubMethod\":\"Restart\"", warning.Line, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Invoke_GameMasterIntentAsDisplay_IsIgnored()
    {
        await using var connection = await HubClients.ConnectAsync(_factory, ProbePath);
        await connection.InvokeAsync(nameof(ProbeHub.Become), Role.Display, TestContext.Current.CancellationToken);

        var result = await connection.InvokeAsync<string?>(nameof(ProbeHub.Restart), TestContext.Current.CancellationToken);

        Assert.Null(result);
        Assert.Equal(0, ProbeHub.Restarts(connection.ConnectionId!));
    }

    [Fact]
    public async Task Invoke_GameMasterIntentAsGameMaster_IsAccepted()
    {
        await using var connection = await HubClients.ConnectAsync(_factory, ProbePath);
        await connection.InvokeAsync(nameof(ProbeHub.Become), Role.GameMaster, TestContext.Current.CancellationToken);

        var result = await connection.InvokeAsync<string?>(nameof(ProbeHub.Restart), TestContext.Current.CancellationToken);

        Assert.Equal("restarted", result);
        Assert.Equal(1, ProbeHub.Restarts(connection.ConnectionId!));
    }

    [Fact]
    public async Task Invoke_MethodThatThrows_AnswersNothingAndLogsError()
    {
        await using var connection = await HubClients.ConnectAsync(_factory, ProbePath);

        var result = await connection.InvokeAsync<string?>(nameof(ProbeHub.Fail), TestContext.Current.CancellationToken);

        Assert.Null(result);
        Assert.Equal(HubConnectionState.Connected, connection.State);
        var error = Assert.Single(LoggedEvent.ReadAll(_logs), e => e.Template.StartsWith("Hub method", StringComparison.Ordinal));
        Assert.Equal("Error", error.Level);
        Assert.Contains(nameof(InvalidOperationException), error.Line, StringComparison.Ordinal);
    }

    /// <summary>
    /// Maps <see cref="ProbeHub"/> ahead of the server's own endpoints.
    /// </summary>
    private sealed class ProbeHubStartup : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.UseRouting();
            app.UseEndpoints(endpoints => endpoints.MapHub<ProbeHub>(ProbePath));
            next(app);
        };
    }
}

/// <summary>
/// A hub with one method per behavior of the filters. Public, as SignalR requires for hub methods.
/// </summary>
internal sealed class ProbeHub : Hub
{
    private static readonly Dictionary<string, int> _restarts = [];

    public static int Restarts(string connectionId)
    {
        lock (_restarts)
        {
            return _restarts.GetValueOrDefault(connectionId);
        }
    }

    public void Become(Role role) => Context.SetRole(role);

    [GameMasterOnly]
    public string Restart()
    {
        lock (_restarts)
        {
            _restarts[Context.ConnectionId] = _restarts.GetValueOrDefault(Context.ConnectionId) + 1;
        }

        return "restarted";
    }

    public string Fail() => throw new InvalidOperationException($"A bug in a hub method, called by {Context.ConnectionId}.");
}
