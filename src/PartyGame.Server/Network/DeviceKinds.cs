using PartyGame.Contracts;

namespace PartyGame.Server.Network;

/// <summary>
/// Deduces the kind of device from the user agent of its browser, for the game master to tell the diagnostics apart. A
/// hint, not an identity: any browser may claim to be another one.
/// </summary>
internal static class DeviceKinds
{
    // The first match wins: Android and iOS user agents also name Linux and Mac OS X.
    private static readonly (string Marker, DeviceKind Kind)[] _markers =
    [
        ("iPhone", DeviceKind.IPhone),
        ("iPad", DeviceKind.IPad),
        ("Android", DeviceKind.Android),
        ("Windows", DeviceKind.Windows),
        ("Macintosh", DeviceKind.Mac),
        ("Linux", DeviceKind.Linux),
    ];

    public static DeviceKind FromUserAgent(string? userAgent) =>
        _markers.FirstOrDefault(m => userAgent?.Contains(m.Marker, StringComparison.Ordinal) == true) is { Marker: not null } match
            ? match.Kind
            : DeviceKind.Other;
}
