namespace PartyGame.Contracts;

/// <summary>
/// The game master chooses the address encoded in the QR code, when the server is on several networks.
/// </summary>
/// <param name="Address">
/// One of the <see cref="GameMasterSnapshot.JoinAddressCandidates"/>: an address the server did not offer is refused.
/// </param>
public sealed record ChooseAdvertisedAddressRequest(string Address);
