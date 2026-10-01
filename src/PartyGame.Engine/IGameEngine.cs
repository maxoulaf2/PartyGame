using PartyGame.Engine.Inputs;

namespace PartyGame.Engine;

/// <summary>
/// The rules of the game, as seen by the game loop. Lets tests replace the engine, for instance with one that throws.
/// </summary>
public interface IGameEngine
{
    /// <summary>
    /// Handles an input. A rejected input yields the same state instance and no effect.
    /// An exception is a bug: the caller keeps the previous state.
    /// </summary>
    /// <param name="state">Current state, left untouched.</param>
    /// <param name="input">The input to handle.</param>
    /// <param name="context">Current time and random generator.</param>
    Transition Handle(GameState state, GameInput input, GameContext context);
}
