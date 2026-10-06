namespace PartyGame.Contracts;

/// <summary>
/// The game master starts the round announced, once its rule is told.
/// </summary>
/// <param name="RoundId">
/// The round announced. A request that names another round is obsolete (double tap, second console, request sent again
/// after a reconnection) and changes nothing: a round is started only once.
/// </param>
public sealed record StartRoundRequest(RoundId RoundId);
