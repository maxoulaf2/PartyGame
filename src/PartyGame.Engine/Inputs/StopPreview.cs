namespace PartyGame.Engine.Inputs;

/// <summary>
/// The game master ends the preview of a pack: the TV screen goes back to the lobby. Only a connection authenticated as game
/// master gets it past the hub.
/// </summary>
/// <param name="ReceivedAt">Server time at which the hub received the intent.</param>
public sealed record StopPreview(DateTimeOffset ReceivedAt) : Intent(ReceivedAt);
