using Microsoft.Extensions.DependencyInjection.Extensions;
using PartyGame.Engine.Modes;

namespace PartyGame.Server.Games;

internal static class GameModeExtensions
{
    /// <summary>
    /// Registers the game modes that play the rounds of the packs, explicitly rather than by scanning assemblies: adding a
    /// mode is one line here, as <c>services.AddSingleton&lt;IGameMode, QuizMode&gt;()</c>.
    /// </summary>
    public static IServiceCollection AddGameModes(this IServiceCollection services)
    {
        // No game mode yet: the quiz comes with E08.
        services.TryAddSingleton(provider => new GameModes(provider.GetServices<IGameMode>()));
        return services;
    }
}
