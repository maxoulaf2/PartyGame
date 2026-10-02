namespace PartyGame.Engine.Inputs;

/// <summary>
/// The game master chooses the address encoded in the QR code. Only a connection authenticated as game master gets it
/// past the hub.
/// </summary>
/// <param name="Address">The chosen address, which must be one of <see cref="GameState.JoinAddressCandidates"/>.</param>
/// <param name="ReceivedAt">Server time at which the hub received the intent.</param>
public sealed record ChooseAdvertisedAddress(string Address, DateTimeOffset ReceivedAt) : Intent(ReceivedAt);
