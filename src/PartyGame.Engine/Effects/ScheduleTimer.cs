namespace PartyGame.Engine.Effects;

/// <summary>
/// Asks for a <see cref="Inputs.TimerElapsed"/> input at a given time. Replaces any timer with the same identifier.
/// </summary>
/// <param name="TimerId">Identifier of the timer.</param>
/// <param name="DueAt">Absolute server time at which the timer elapses, computed from <see cref="GameContext.Now"/>.</param>
public sealed record ScheduleTimer(TimerId TimerId, DateTimeOffset DueAt) : Effect;
