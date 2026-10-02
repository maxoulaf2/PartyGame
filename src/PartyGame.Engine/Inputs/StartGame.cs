namespace PartyGame.Engine.Inputs;

/// <summary>
/// The game master starts the game. Only a connection authenticated as game master gets it past the hub.
/// </summary>
/// <param name="ReceivedAt">Server time at which the hub received the intent.</param>
public sealed record StartGame(DateTimeOffset ReceivedAt) : Intent(ReceivedAt);
