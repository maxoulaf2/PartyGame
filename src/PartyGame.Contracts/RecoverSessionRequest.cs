namespace PartyGame.Contracts;

/// <summary>
/// A phone that lost its token, or a new phone or browser, asks to become an already registered player with the
/// reconnection code the game master reads them from their console.
/// </summary>
/// <param name="Code">The reconnection code of the player, as typed: case and surrounding spaces do not matter.</param>
public sealed record RecoverSessionRequest(string Code);
