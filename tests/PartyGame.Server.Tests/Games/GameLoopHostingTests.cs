using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PartyGame.Contracts;
using PartyGame.Engine;
using PartyGame.Engine.Inputs;
using PartyGame.Server.Games;

namespace PartyGame.Server.Tests.Games;

public sealed class GameLoopHostingTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task StartAsync_ServerStarted_RunsSingleLoopWithLobbyAndRealEngine()
    {
        // Given
        var services = factory.Services;
        var loop = services.GetRequiredService<GameLoop>();

        // When
        var outcome = await services.GetRequiredService<IGameInputWriter>().SubmitAsync(
            new JoinGame(new PlayerId(Guid.NewGuid()), new PlayerToken("token"), "Zoé", DateTimeOffset.UnixEpoch),
            TestContext.Current.CancellationToken);

        // Then
        Assert.Contains(loop, services.GetServices<IHostedService>());
        Assert.Same(loop, services.GetRequiredService<IGameInputWriter>());
        Assert.Equal(InputOutcome.Accepted, outcome);
        Assert.Equal(GamePhase.Lobby, loop.State.Phase);
        Assert.Equal("Zoé", Assert.Single(loop.State.Players).Nickname);
    }
}
