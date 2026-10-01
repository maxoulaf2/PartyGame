using Microsoft.Extensions.DependencyInjection.Extensions;
using PartyGame.Contracts;
using PartyGame.Engine;

namespace PartyGame.Server.Games;

internal static class GameLoopExtensions
{
    /// <summary>
    /// Runs the single game of this server in a <see cref="GameLoop"/>, and exposes its queue to the producers of inputs.
    /// </summary>
    public static WebApplicationBuilder AddGameLoop(this WebApplicationBuilder builder)
    {
        builder.Services.TryAddSingleton(TimeProvider.System);
        builder.Services.TryAddSingleton<IGameEngine, GameEngine>();
        builder.Services.TryAddSingleton<IEffectExecutor, UnsupportedEffectExecutor>();

        // The engine has no randomness of its own: the identifier of the game and the seed come from here.
        builder.Services.AddSingleton(services => new GameLoop(
            GameState.Create(new GameId(Guid.NewGuid())),
            Random.Shared.Next(),
            services.GetRequiredService<IGameEngine>(),
            services.GetRequiredService<TimeProvider>(),
            services.GetRequiredService<IEffectExecutor>(),
            services.GetServices<IGameStateListener>(),
            services.GetRequiredService<ILogger<GameLoop>>()));
        builder.Services.AddSingleton<IGameInputWriter>(services => services.GetRequiredService<GameLoop>());
        builder.Services.AddHostedService(services => services.GetRequiredService<GameLoop>());

        return builder;
    }
}
