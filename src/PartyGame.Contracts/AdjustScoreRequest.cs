namespace PartyGame.Contracts;

/// <summary>
/// The game master corrects the score of a player, to repair a misjudgement, give a bonus or bring a latecomer back into the
/// race.
/// </summary>
/// <param name="PlayerId">The player whose score changes.</param>
/// <param name="ExpectedScore">
/// The score the game master corrects, as the console shows it: a request sent twice, or by a second console that does not
/// see the last score, is rejected as obsolete.
/// </param>
/// <param name="NewScore">The new total of the player, never negative.</param>
public sealed record AdjustScoreRequest(PlayerId PlayerId, int ExpectedScore, int NewScore);
