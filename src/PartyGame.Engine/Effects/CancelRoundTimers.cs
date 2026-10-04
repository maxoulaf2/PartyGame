using PartyGame.Contracts;

namespace PartyGame.Engine.Effects;

/// <summary>
/// Cancels every timer a round scheduled, whose identifiers only its game mode knows: when the round ends without it. Their
/// input may still arrive if one already elapsed: the engine then rejects it, the round being over.
/// </summary>
/// <param name="RoundId">The round whose timers are cancelled, as <see cref="ScheduleTimer.RoundId"/> marks them.</param>
public sealed record CancelRoundTimers(RoundId RoundId) : Effect;
