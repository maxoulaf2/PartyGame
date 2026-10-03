namespace PartyGame.Contracts;

/// <summary>
/// Where one player stands in the ranking of the game, as their phone shows it between two rounds and once the game is
/// finished: their rank alone, never the nicknames nor the scores of the others.
/// </summary>
/// <param name="Rank">
/// Rank of the player, from 1: one more than the number of players with more points, so that players with the same score
/// share it.
/// </param>
/// <param name="IsTied">Whether another player shares <paramref name="Rank"/>.</param>
/// <param name="RankedCount">
/// Number of ranked players, this one included: every registered player, but those who joined once the game was
/// finished.
/// </param>
public sealed record PlayerStanding(int Rank, bool IsTied, int RankedCount);
