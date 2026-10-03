namespace PartyGame.Contracts;

/// <summary>
/// Where one player stands in the ranking of the game, as their phone shows it between two rounds: their rank alone,
/// never the nicknames nor the scores of the others.
/// </summary>
/// <param name="Rank">
/// Rank of the player, from 1: one more than the number of players with more points, so that players with the same score
/// share it. Out of <see cref="PlayerSnapshot.PlayerCount"/>, every registered player being ranked.
/// </param>
/// <param name="IsTied">Whether another player shares <paramref name="Rank"/>.</param>
public sealed record PlayerStanding(int Rank, bool IsTied);
