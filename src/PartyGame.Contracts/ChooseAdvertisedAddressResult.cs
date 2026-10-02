namespace PartyGame.Contracts;

/// <summary>
/// The answer of the hub to a <see cref="ChooseAdvertisedAddressRequest"/>. The new address itself reaches the TV screen
/// and the console through the snapshots.
/// </summary>
/// <param name="Refusal">Why the choice was refused, or <see langword="null"/> when the address is advertised.</param>
public sealed record ChooseAdvertisedAddressResult(ChooseAdvertisedAddressRefusal? Refusal);
