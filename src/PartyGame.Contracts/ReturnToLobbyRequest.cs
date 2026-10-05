namespace PartyGame.Contracts;

/// <summary>
/// The game master ends the game, whatever its phase, and goes back to the lobby with the same players, their scores reset,
/// to start a new game without restarting the server.
/// </summary>
/// <param name="GameId">
/// The game to end, which must be the current one. A request that names another game is obsolete (double tap, second
/// console, request sent again after a reconnection) and changes nothing: the new game is never ended by it.
/// </param>
public sealed record ReturnToLobbyRequest(GameId GameId);
