namespace PartyGame.Contracts;

/// <summary>
/// The kind of device a page runs on, as the server deduces it from the user agent of the browser, for the game master to
/// tell the diagnostics apart.
/// </summary>
public enum DeviceKind
{
    /// <summary>An iPhone.</summary>
    IPhone,

    /// <summary>An iPad that tells it is one: recent ones claim to be a Mac.</summary>
    IPad,

    /// <summary>An Android phone or tablet.</summary>
    Android,

    /// <summary>A Windows PC.</summary>
    Windows,

    /// <summary>A Mac, or a recent iPad.</summary>
    Mac,

    /// <summary>A Linux computer, such as the Raspberry Pi of the TV screen.</summary>
    Linux,

    /// <summary>A browser the server does not recognize.</summary>
    Other,
}
