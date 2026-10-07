using PartyGame.Engine.State;

namespace PartyGame.Engine.Scores;

/// <summary>
/// Where a player stands in the ranking of the game.
/// </summary>
/// <param name="Player">The player.</param>
/// <param name="Rank">
/// Rank of the player, from 1: one more than the number of players with more points, so that players with the same score
/// share it and the next rank counts them (1, 1, 3).
/// </param>
/// <param name="IsTied">Whether another player shares <paramref name="Rank"/>.</param>
public sealed record Standing(Player Player, int Rank, bool IsTied);
