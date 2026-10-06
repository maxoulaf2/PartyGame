namespace PartyGame.Contracts;

/// <summary>
/// A round of the programme that is not played yet, as the game master orders it.
/// </summary>
/// <param name="RoundIndex">Position of its activity in the pack, from 0.</param>
/// <param name="IsWithdrawn">Whether the round is withdrawn from the programme: it is not played unless put back.</param>
public sealed record ScheduledRound(int RoundIndex, bool IsWithdrawn);
