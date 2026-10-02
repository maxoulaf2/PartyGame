using System.Collections.Immutable;
using PartyGame.Engine.Effects;

namespace PartyGame.Engine.Modes;

/// <summary>
/// Result of a game mode starting a round or handling an input aimed at it, on the model of <see cref="Transition"/>.
/// </summary>
/// <param name="State">
/// The new state of the round, or the very same instance when the input is rejected or changes nothing.
/// </param>
/// <param name="Effects">
/// What must happen outside the engine, in order. The engine marks the timers with the round, so that one that elapses
/// once the round is over never reaches the mode.
/// </param>
public sealed record RoundTransition(RoundState State, ImmutableArray<Effect> Effects)
{
    /// <summary>
    /// Whether the round is over. The game then goes between two rounds, or is finished after the last one.
    /// </summary>
    public bool IsFinished { get; init; }

    /// <summary>
    /// Why the input was rejected, or <see langword="null"/> when it was accepted.
    /// </summary>
    public RejectionReason? Rejection { get; private init; }

    /// <summary>
    /// Rejects an input: same state instance, no effect.
    /// </summary>
    /// <param name="state">The state of the round the input was handled in.</param>
    /// <param name="reason">Why the input is rejected.</param>
    public static RoundTransition Rejected(RoundState state, RejectionReason reason) =>
        new(state, []) { Rejection = reason };
}
