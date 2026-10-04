using PartyGame.Engine;
using PartyGame.Engine.Inputs;
using PartyGame.Server.Logging;

namespace PartyGame.Server.Faults;

internal static class FaultInjectionExtensions
{
    /// <summary>
    /// Makes the engine throw on the input <see cref="FaultInjectionOptions.FailOnInputSetting"/> names, to check by hand
    /// or in the E2E tests that a bug of a game mode stays invisible to the players. Never in the <c>Production</c>
    /// environment, whatever the configuration: a server launched for an evening must not fail on purpose. A name that
    /// matches no input stops the server, rather than leaving a test believing a fault was injected.
    /// </summary>
    public static WebApplicationBuilder AddFaultInjection(this WebApplicationBuilder builder)
    {
        var options = FaultInjectionOptions.Read(builder.Configuration);
        if (!options.IsRequested || builder.Environment.IsProduction())
        {
            return builder;
        }

        var inputs = typeof(GameInput).Assembly.GetTypes().Where(type => type.IsSubclassOf(typeof(GameInput)) && !type.IsAbstract).Select(type => type.Name);
        if (!inputs.Contains(options.FailOnInput))
        {
            throw new InvalidOperationException(
                $"{FaultInjectionOptions.FailOnInputSetting} names no input: {options.FailOnInput}. Inputs: {string.Join(", ", inputs.Order(StringComparer.Ordinal))}");
        }

        // Registered before the game loop, whose own registration then gives way.
        builder.Services.AddSingleton<IGameEngine>(services =>
            new FaultInjectingEngine(ActivatorUtilities.CreateInstance<GameEngine>(services), options.FailOnInput!, options.FailCount));

        return builder;
    }

    /// <summary>Warns the operator, at startup, of a fault injection requested: one left on by mistake must show.</summary>
    public static void LogFaultInjection(this WebApplication app)
    {
        var options = FaultInjectionOptions.Read(app.Configuration);
        if (!options.IsRequested)
        {
            return;
        }

        if (app.Environment.IsProduction())
        {
            app.Logger.FaultInjectionIgnored(FaultInjectionOptions.FailOnInputSetting);
        }
        else
        {
            app.Logger.FaultInjectionActive(options.FailOnInput!, options.FailCount);
        }
    }
}
