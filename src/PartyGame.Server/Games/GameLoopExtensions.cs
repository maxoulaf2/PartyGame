using Microsoft.Extensions.DependencyInjection.Extensions;
using PartyGame.Content;
using PartyGame.Contracts;
using PartyGame.Engine;
using PartyGame.Engine.Projections;
using PartyGame.Server.Incidents;
using PartyGame.Server.Network;
using PartyGame.Server.Packs;
using PartyGame.Server.Persistence;

namespace PartyGame.Server.Games;

internal static class GameLoopExtensions
{
    /// <summary>
    /// Runs the single game of this server in a <see cref="GameLoop"/> with the registered game modes, and exposes its
    /// queue to the producers of inputs.
    /// </summary>
    public static WebApplicationBuilder AddGameLoop(this WebApplicationBuilder builder)
    {
        builder.Services.TryAddSingleton(TimeProvider.System);
        builder.Services.AddGameModes();
        builder.Services.TryAddSingleton<IGameEngine, GameEngine>();
        builder.Services.TryAddSingleton<Snapshots>();
        builder.Services.TryAddSingleton<MediaLocator>();
        builder.Services.AddSingleton<IGameStateListener, RoundProgressLog>();
        builder.AddGamePersistence();
        builder.Services.TryAddSingleton<GameInputQueue>();
        builder.Services.TryAddSingleton<IGameInputWriter>(services => services.GetRequiredService<GameInputQueue>());
        builder.Services.TryAddSingleton<TimerScheduler>();
        builder.Services.TryAddSingleton<IEffectExecutor, EffectExecutor>();
        builder.Services.TryAddSingleton<IncidentJournal>();
        builder.Services.TryAddSingleton<IIncidentReporter, IncidentReporter>();

        // The engine has no randomness of its own: the identifier of the game and the seed come from here. Neither does it
        // know the network: the address phones join at, and those the game master may choose instead, come from the
        // selection made at startup. A choice of the game master lives in the state only, and is forgotten on restart. The
        // packs, loaded before the server listens, are in the state from the start: no console ever sees an empty catalog.
        builder.Services.AddSingleton(services =>
        {
            var selection = services.GetRequiredService<AddressSelection>();
            return new GameLoop(
                GameState.Create(
                    new GameId(Guid.NewGuid()),
                    selection.Address?.ToString(),
                    selection.ToJoinAddressCandidates(),
                    services.GetRequiredService<PackLibrary>().ToCatalog()),
                Random.Shared.Next(),
                services.GetRequiredService<GameInputQueue>(),
                services.GetRequiredService<IGameEngine>(),
                services.GetRequiredService<TimeProvider>(),
                services.GetRequiredService<IEffectExecutor>(),
                services.GetServices<IGameStateListener>(),
                services.GetRequiredService<IIncidentReporter>(),
                services.GetRequiredService<ILogger<GameLoop>>());
        });
        builder.Services.AddHostedService(services => services.GetRequiredService<GameLoop>());

        return builder;
    }
}
