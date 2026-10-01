using System.Collections.Immutable;
using PartyGame.Engine.Effects;

namespace PartyGame.Engine;

/// <summary>
/// Result of handling an input: the new state, and the effects that the game loop executes outside the engine.
/// </summary>
/// <param name="State">The new state, or the very same instance as the given state when the input is rejected.</param>
/// <param name="Effects">What must happen outside the engine, in order.</param>
public sealed record Transition(GameState State, ImmutableArray<Effect> Effects)
{
    /// <summary>
    /// Why the input was rejected, or <see langword="null"/> when it was accepted.
    /// The game loop logs it, and answers it to the sender of an input that waits for an answer.
    /// </summary>
    public RejectionReason? Rejection { get; private init; }

    /// <summary>
    /// Rejects an input: same state instance, no effect.
    /// </summary>
    /// <param name="state">The state the input was handled in.</param>
    /// <param name="reason">Why the input is rejected.</param>
    public static Transition Rejected(GameState state, RejectionReason reason) =>
        new(state, []) { Rejection = reason };
}
