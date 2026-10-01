namespace PartyGame.Engine.Inputs;

/// <summary>
/// A timer scheduled by <see cref="Effects.ScheduleTimer"/> elapsed.
/// </summary>
/// <param name="TimerId">Identifier of the timer.</param>
public sealed record TimerElapsed(TimerId TimerId) : GameInput;
