namespace PartyGame.Contracts;

/// <summary>
/// A player in the ranking of the game, as the TV screen and the game master console show it between two rounds and once
/// the game is finished. The server ranks the players: a client neither sorts nor ranks anything.
/// </summary>
/// <param name="Id">Identifier of the player, which lets a client follow them through a rename.</param>
/// <param name="Nickname">Nickname of the player, to show as plain text.</param>
/// <param name="IsConnected">Whether the phone of the player is connected. A disconnected player stays ranked.</param>
/// <param name="Rank">
/// Rank of the player, from 1: one more than the number of players with more points, so that players with the same score
/// share it and the next rank counts them (1, 1, 3).
/// </param>
/// <param name="IsTied">Whether another player shares <paramref name="Rank"/>.</param>
/// <param name="Score">The points of the player since the start of the game.</param>
/// <param name="PreviousRank">
/// Rank of the player in the ranking before this one, to show who gained or lost places; <see langword="null"/> in the
/// ranking after the first round, and for a player who joined since the previous ranking.
/// </param>
public sealed record RankedPlayer(PlayerId Id, string Nickname, bool IsConnected, int Rank, bool IsTied, int Score, int? PreviousRank);
