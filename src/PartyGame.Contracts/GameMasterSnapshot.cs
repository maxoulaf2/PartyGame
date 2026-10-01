namespace PartyGame.Contracts;

/// <summary>
/// What the game master console shows. The only projection allowed to hold the answers before their reveal; it never
/// holds the game master code nor the tokens of the players.
/// </summary>
/// <param name="GameId">The game the snapshot belongs to. A client that receives another one starts over.</param>
/// <param name="Version">Increases by one with each change of the game, so that a client ignores older snapshots.</param>
/// <param name="Phase">Current phase of the game.</param>
/// <param name="PlayerCount">Number of registered players.</param>
public sealed record GameMasterSnapshot(GameId GameId, long Version, Phase Phase, int PlayerCount);
