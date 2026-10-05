using PartyGame.Contracts;

namespace PartyGame.Engine.Inputs;

/// <summary>
/// The game master ends the game and goes back to the lobby. Only a connection authenticated as game master gets it past
/// the hub.
/// </summary>
/// <param name="GameId">The game to end: the request is obsolete once the lobby has a new game, or for another game.</param>
/// <param name="ReceivedAt">Server time at which the hub received the intent.</param>
public sealed record ReturnToLobby(GameId GameId, DateTimeOffset ReceivedAt) : Intent(ReceivedAt);
