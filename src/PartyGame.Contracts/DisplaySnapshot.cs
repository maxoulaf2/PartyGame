namespace PartyGame.Contracts;

/// <summary>
/// What the TV screen shows. Public by construction: anybody on the network can open the TV page, so it never holds
/// an answer before its reveal nor any secret.
/// </summary>
/// <param name="GameId">The game the snapshot belongs to. A client that receives another one starts over.</param>
/// <param name="Version">Increases by one with each change of the game, so that a client ignores older snapshots.</param>
/// <param name="Phase">Current phase of the game.</param>
/// <param name="PlayerCount">Number of registered players.</param>
public sealed record DisplaySnapshot(GameId GameId, long Version, Phase Phase, int PlayerCount);
