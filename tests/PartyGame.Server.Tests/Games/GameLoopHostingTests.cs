using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PartyGame.Contracts;
using PartyGame.Engine;
using PartyGame.Engine.Inputs;
using PartyGame.Server.Games;

namespace PartyGame.Server.Tests.Games;

public sealed class GameLoopHostingTests : IAsyncDisposable
{
    private readonly TempDirectory _scratch = new();
    private readonly WebApplicationFactory<Program> _factory;

    // A folder of its own: the game it saves must not be offered to another test server.
    public GameLoopHostingTests() =>
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder.UseScratchDirectory(_scratch.Path));

    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
        _scratch.Dispose();
    }

    [Fact]
    public async Task StartAsync_ServerStarted_RunsSingleLoopWithLobbyAndRealEngine()
    {
        // Given
        var services = _factory.Services;
        var loop = services.GetRequiredService<GameLoop>();

        // When
        var outcome = await services.GetRequiredService<IGameInputWriter>().SubmitAsync(
            new JoinGame(new PlayerId(Guid.NewGuid()), new PlayerToken("token"), "Zoé", DateTimeOffset.UnixEpoch),
            TestContext.Current.CancellationToken);

        // Then
        Assert.Contains(loop, services.GetServices<IHostedService>());
        Assert.Same(services.GetRequiredService<GameInputQueue>(), services.GetRequiredService<IGameInputWriter>());
        Assert.IsType<EffectExecutor>(services.GetRequiredService<IEffectExecutor>());
        Assert.Equal(InputOutcome.Accepted, outcome);
        Assert.Equal(GamePhase.Lobby, loop.State.Phase);
        Assert.Equal("Zoé", Assert.Single(loop.State.Players).Nickname);
    }
}
