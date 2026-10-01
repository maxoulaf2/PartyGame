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
/// <param name="PlayerCount">Number of registered players.</param>
public sealed record PlayerSnapshot(GameId GameId, long Version, Phase Phase, PlayerId PlayerId, string Nickname, int PlayerCount);
