namespace PartyGame.Contracts;

/// <summary>
/// The game master asks for the next round, between two rounds.
/// </summary>
/// <param name="AfterRound">
/// The round that just finished. A request that names another round is obsolete (double tap, second console, request
/// sent again after a reconnection) and changes nothing: a round is never skipped by mistake.
/// </param>
public sealed record NextRoundRequest(RoundId AfterRound);
