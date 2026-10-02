using PartyGame.Contracts;

namespace PartyGame.Engine.Effects;

/// <summary>
/// Asks for a <see cref="Inputs.TimerElapsed"/> input at a given time. Replaces any timer with the same identifier.
/// </summary>
/// <param name="TimerId">Identifier of the timer.</param>
/// <param name="DueAt">Absolute server time at which the timer elapses, computed from <see cref="GameContext.Now"/>.</param>
public sealed record ScheduleTimer(TimerId TimerId, DateTimeOffset DueAt) : Effect
{
    /// <summary>
    /// The round that scheduled the timer, set by the engine on every timer of a game mode, or <see langword="null"/> for
    /// a timer of the engine itself. Carried back by <see cref="Inputs.TimerElapsed.RoundId"/>.
    /// </summary>
    public RoundId? RoundId { get; init; }
}
