namespace PartyGame.Engine.Inputs;

/// <summary>
/// The game master shows or hides the QR code on the TV screen outside the lobby. Only a connection authenticated as game
/// master gets it past the hub.
/// </summary>
/// <param name="Shown">Whether the QR code is shown.</param>
/// <param name="ReceivedAt">Server time at which the hub received the intent.</param>
public sealed record ShowJoinCode(bool Shown, DateTimeOffset ReceivedAt) : Intent(ReceivedAt);
