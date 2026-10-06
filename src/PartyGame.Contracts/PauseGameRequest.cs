namespace PartyGame.Contracts;

/// <summary>
/// The game master pauses the game, or resumes it where it stood: countdowns and the excerpt of the TV screen stop, and
/// every intent aimed at a round is refused meanwhile.
/// </summary>
/// <param name="GameId">
/// The game to pause or resume, which must be the current one: a request for a game ended since changes nothing.
/// </param>
/// <param name="Paused">
/// Whether the game is paused. A value rather than a toggle: a request sent twice (double tap, second console, request
/// sent again after a reconnection) is rejected as obsolete once the game is as the game master asked.
/// </param>
public sealed record PauseGameRequest(GameId GameId, bool Paused);
