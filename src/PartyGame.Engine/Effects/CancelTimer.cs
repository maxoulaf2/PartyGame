namespace PartyGame.Engine.Effects;

/// <summary>
/// Cancels a timer scheduled by <see cref="ScheduleTimer"/>. Its input may still arrive if it already elapsed:
/// the engine then rejects it as obsolete.
/// </summary>
/// <param name="TimerId">Identifier of the timer.</param>
public sealed record CancelTimer(TimerId TimerId) : Effect;
