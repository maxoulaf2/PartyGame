using PartyGame.Contracts;

namespace PartyGame.Engine.Inputs;

/// <summary>
/// A timer scheduled by <see cref="Effects.ScheduleTimer"/> elapsed. It may arrive after the timer was cancelled or replaced:
/// the engine then rejects it as obsolete.
/// </summary>
/// <param name="TimerId">Identifier of the timer.</param>
/// <param name="DueAt">
/// The due time of the <see cref="Effects.ScheduleTimer"/> that elapsed, so that the engine can tell the input of a replaced
/// timer from the one of the timer it now waits for, since both have the same identifier.
/// </param>
public sealed record TimerElapsed(TimerId TimerId, DateTimeOffset DueAt) : GameInput
{
    /// <summary>
    /// The round that scheduled the timer, copied from <see cref="Effects.ScheduleTimer.RoundId"/>. The engine rejects the
    /// timer of a round that is over before its game mode sees it.
    /// </summary>
    public RoundId? RoundId { get; init; }
}
