using Microsoft.Extensions.Options;
using PartyGame.Server.Games;

namespace PartyGame.Server.Persistence;

internal static class PersistenceExtensions
{
    /// <summary>
    /// Saves the game after each change. Must come before the hosted service of the loop: hosted services stop in reverse
    /// order, and the last state of the loop is saved once the loop stopped.
    /// </summary>
    public static WebApplicationBuilder AddGamePersistence(this WebApplicationBuilder builder)
    {
        builder.Services.AddOptions<PersistenceOptions>()
            .BindConfiguration(PersistenceOptions.SectionName)
            .Validate(options => !string.IsNullOrWhiteSpace(options.Directory), $"{PersistenceOptions.DirectorySetting} must not be empty")
            .ValidateOnStart();

        builder.Services.AddSingleton<SavedGameLoader>();
        builder.Services.AddSingleton<GamePersistence>();
        builder.Services.AddSingleton<IGameStateListener>(services => services.GetRequiredService<GamePersistence>());
        builder.Services.AddHostedService(services => services.GetRequiredService<GamePersistence>());

        return builder;
    }

    /// <summary>
    /// Checks the folder the game is saved to before the server listens: a game that could not be resumed is no game to
    /// start.
    /// </summary>
    /// <exception cref="DataDirectoryException">The folder cannot be used.</exception>
    public static void PrepareDataDirectory(this WebApplication app) =>
        GamePersistence.PrepareDirectory(
            app.Services.GetRequiredService<IOptions<PersistenceOptions>>().Value.FullDirectory,
            app.Services.GetRequiredService<ILogger<GamePersistence>>());
}
