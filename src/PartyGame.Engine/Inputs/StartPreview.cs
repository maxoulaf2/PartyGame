namespace PartyGame.Engine.Inputs;

/// <summary>
/// The game master previews a pack on the TV screen, in the lobby. Only a connection authenticated as game master gets it
/// past the hub.
/// </summary>
/// <param name="PackId">The identifier of a valid pack of <see cref="GameState.Catalog"/>.</param>
/// <param name="ReceivedAt">Server time at which the hub received the intent.</param>
public sealed record StartPreview(string PackId, DateTimeOffset ReceivedAt) : Intent(ReceivedAt);
