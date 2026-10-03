namespace PartyGame.Contracts;

/// <summary>
/// What the phone of one player shows: only what is public or meant for this player, never what other players did
/// before the reveal.
/// </summary>
/// <param name="GameId">The game the snapshot belongs to. A client that receives another one starts over.</param>
/// <param name="Version">Increases by one with each change of the game, so that a client ignores older snapshots.</param>
/// <param name="Phase">Current phase of the game.</param>
/// <param name="PlayerId">The player the snapshot is meant for.</param>
/// <param name="Nickname">Nickname of this player.</param>
/// <param name="Score">
/// The points of this player since the start of the game, computed by the server alone: 0 until their first points.
/// </param>
/// <param name="PlayerCount">Number of registered players.</param>
/// <param name="Round">
/// The round in progress, or the round that just finished between two rounds and once the game is finished, or
/// <see langword="null"/> before the first round.
/// </param>
/// <param name="RoundView">
/// What the game mode of the round in progress shows on the phone of this player, or <see langword="null"/> outside a
/// round. A player who joins during a round gets it too: the game mode decides whether they take part.
/// </param>
/// <param name="Standing">
/// Where this player stands in the ranking, between two rounds and once the game is finished; <see langword="null"/>
/// otherwise, and for a player who joined once the game was finished, who played no round.
/// </param>
public sealed record PlayerSnapshot(
    GameId GameId,
    long Version,
    Phase Phase,
    PlayerId PlayerId,
    string Nickname,
    int Score,
    int PlayerCount,
    RoundInfo? Round,
    PlayerRoundView? RoundView,
    PlayerStanding? Standing);
