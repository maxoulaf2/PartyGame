using System.Collections.Immutable;
using PartyGame.Engine.Effects;

namespace PartyGame.Engine.Buzzers;

/// <summary>
/// Result of a buzzer handling a buzz or the end of its arbitration window, on the model of <see cref="Transition"/>.
/// </summary>
/// <param name="Buzzer">The new buzzer, or the very same instance when the input is rejected.</param>
/// <param name="Effects">What the round must do outside the engine: the timer of the arbitration window.</param>
public sealed record BuzzerTransition(Buzzer Buzzer, ImmutableArray<Effect> Effects)
{
    /// <summary>
    /// Why the input was rejected, or <see langword="null"/> when it was accepted.
    /// </summary>
    public RejectionReason? Rejection { get; private init; }

    /// <summary>
    /// Rejects an input: same buzzer instance, no effect.
    /// </summary>
    /// <param name="buzzer">The buzzer the input was handled by.</param>
    /// <param name="reason">Why the input is rejected.</param>
    public static BuzzerTransition Rejected(Buzzer buzzer, RejectionReason reason) => new(buzzer, []) { Rejection = reason };
}
