namespace PartyGame.Contracts;

/// <summary>
/// Why the hub refused a <see cref="ChooseAdvertisedAddressRequest"/>. The console shows the address of the snapshot either way.
/// </summary>
public enum ChooseAdvertisedAddressRefusal
{
    /// <summary>The address is not one of the candidates offered by the server.</summary>
    AddressUnknown,

    /// <summary>The server could not handle the choice; the game master may try again.</summary>
    ChoiceFailed,

    /// <summary>The message does not have the expected shape.</summary>
    MessageInvalid,
}
