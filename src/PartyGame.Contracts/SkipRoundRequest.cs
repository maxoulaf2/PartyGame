namespace PartyGame.Contracts;

/// <summary>
/// The game master skips the round in progress, without its game mode: offered when the round keeps failing.
/// </summary>
/// <param name="RoundId">
/// The round to skip, which must be the one in progress. A request that names another round is obsolete (double tap,
/// second console, request sent again after a reconnection) and changes nothing: a single round is ever skipped.
/// </param>
public sealed record SkipRoundRequest(RoundId RoundId);
