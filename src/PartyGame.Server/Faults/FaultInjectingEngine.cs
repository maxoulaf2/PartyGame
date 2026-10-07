using PartyGame.Engine;
using PartyGame.Engine.Inputs;
using PartyGame.Engine.State;

namespace PartyGame.Server.Faults;

/// <summary>
/// Throws instead of handling the inputs of the type <paramref name="failOnInput"/> names, <paramref name="failCount"/>
/// times, as a bug of a game mode would: what the game loop then does, and what each screen shows, can be checked on
/// purpose.
/// </summary>
internal sealed class FaultInjectingEngine(IGameEngine engine, string failOnInput, int failCount) : IGameEngine
{
    // Only the game loop calls the engine: no lock.
    private int _remaining = failCount;

    public Transition Handle(GameState state, GameInput input, GameContext context)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (_remaining > 0 && input.GetType().Name == failOnInput)
        {
            _remaining--;
            throw new InvalidOperationException($"Fault injected on {failOnInput}");
        }

        return engine.Handle(state, input, context);
    }
}
