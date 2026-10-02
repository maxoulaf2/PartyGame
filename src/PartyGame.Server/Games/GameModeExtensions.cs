using Microsoft.Extensions.DependencyInjection.Extensions;
using PartyGame.Engine.Modes;
using PartyGame.Engine.Modes.Quiz;

namespace PartyGame.Server.Games;

internal static class GameModeExtensions
{
    /// <summary>
    /// Registers the game modes that play the rounds of the packs, explicitly rather than by scanning assemblies: adding a
    /// mode is one line here. Idempotent: both the game loop and the loading of the packs need the modes.
    /// </summary>
    public static IServiceCollection AddGameModes(this IServiceCollection services)
    {
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IGameMode, QuizMode>());
        services.TryAddSingleton(provider => new GameModes(provider.GetServices<IGameMode>()));
        return services;
    }
}
