using PartyGame.Contracts;

namespace PartyGame.Engine.Inputs;

/// <summary>
/// The game master starts a new game rather than resume the one the server found saved when it restarted. Only a
/// connection authenticated as game master gets it past the hub.
/// </summary>
/// <param name="SavedGameId">The game found: the request is obsolete once the decision is made, or for another game.</param>
/// <param name="ReceivedAt">Server time at which the hub received the intent.</param>
public sealed record DiscardSavedGame(GameId SavedGameId, DateTimeOffset ReceivedAt) : Intent(ReceivedAt);
