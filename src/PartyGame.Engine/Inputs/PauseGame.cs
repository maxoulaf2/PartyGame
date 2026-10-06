using PartyGame.Contracts;

namespace PartyGame.Engine.Inputs;

/// <summary>
/// The game master pauses the game, or resumes it. Only a connection authenticated as game master gets it past the hub.
/// </summary>
/// <param name="GameId">The game to pause or resume: the request is obsolete for another game.</param>
/// <param name="Paused">Whether the game is paused.</param>
/// <param name="ReceivedAt">Server time at which the hub received the intent.</param>
public sealed record PauseGame(GameId GameId, bool Paused, DateTimeOffset ReceivedAt) : Intent(ReceivedAt);
