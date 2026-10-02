using Microsoft.Extensions.DependencyInjection.Extensions;
using PartyGame.Contracts;
using PartyGame.Engine;
using PartyGame.Server.Network;

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
        builder.Services.TryAddSingleton<GameInputQueue>();
        builder.Services.TryAddSingleton<IGameInputWriter>(services => services.GetRequiredService<GameInputQueue>());
        builder.Services.TryAddSingleton<TimerScheduler>();
        builder.Services.TryAddSingleton<IEffectExecutor, EffectExecutor>();

        // The engine has no randomness of its own: the identifier of the game and the seed come from here. Neither does it
        // know the network: the address phones join at comes from the selection made at startup.
        builder.Services.AddSingleton(services => new GameLoop(
            GameState.Create(new GameId(Guid.NewGuid()), services.GetRequiredService<AddressSelection>().Address?.ToString()),
            Random.Shared.Next(),
            services.GetRequiredService<GameInputQueue>(),
            services.GetRequiredService<IGameEngine>(),
            services.GetRequiredService<TimeProvider>(),
            services.GetRequiredService<IEffectExecutor>(),
            services.GetServices<IGameStateListener>(),
            services.GetRequiredService<ILogger<GameLoop>>()));
        builder.Services.AddHostedService(services => services.GetRequiredService<GameLoop>());

        return builder;
    }
}
