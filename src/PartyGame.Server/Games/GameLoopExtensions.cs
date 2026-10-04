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
        // A game saved by the previous run waits for the game master to resume it, its media files checked beforehand.
        builder.Services.AddSingleton(services =>
        {
            var selection = services.GetRequiredService<AddressSelection>();
            var gameId = new GameId(Guid.NewGuid());
            var address = selection.Address?.ToString();
            var candidates = selection.ToJoinAddressCandidates();
            var catalog = services.GetRequiredService<PackLibrary>().ToCatalog();
            var saved = services.GetRequiredService<SavedGameLoader>().Load();
            return new GameLoop(
                saved is null
                    ? GameState.Create(gameId, address, candidates, catalog)
                    : GameState.CreateResumePending(
                        gameId,
                        address,
                        candidates,
                        catalog,
                        new PendingGame(saved.Game, saved.SavedAt, PackMediaFiles.Missing(saved.Game))),
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

    /// <summary>
    /// Creates the game now, before the server listens and once the data directory is ready: the game saved by the previous
    /// run is read then, and the banner tells whether it waits for the game master.
    /// </summary>
    public static GameLoop LoadGame(this WebApplication app) => app.Services.GetRequiredService<GameLoop>();
}
