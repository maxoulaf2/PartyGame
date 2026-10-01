using System.Net;
using System.Net.Sockets;

namespace PartyGame.Server.Network;

internal sealed class NetworkOptions
{
    public const string SectionName = "Network";

    public const int DefaultPort = 5000;

    // HTTP port listened to on every IPv4 interface. Other services (startup banner, QR code)
    // read it from here rather than parsing the listening URLs.
    public int Port { get; init; } = DefaultPort;

    // IPv4 address advertised to phones instead of the detected one, for hosts with several active networks.
    public string? AdvertisedAddress { get; init; }

    public static NetworkOptions Read(IConfiguration configuration) =>
        configuration.GetSection(SectionName).Get<NetworkOptions>() ?? new NetworkOptions();

    /// <summary>
    /// Parses <see cref="AdvertisedAddress"/>. Only the dotted form is accepted: <see cref="IPAddress.TryParse(string?, out IPAddress?)"/>
    /// also takes shorthands such as "10.1", which would put a surprising address in the QR code.
    /// </summary>
    public bool TryGetAdvertisedAddress(out IPAddress? address)
    {
        address = null;
        if (string.IsNullOrWhiteSpace(AdvertisedAddress))
        {
            return true;
        }

        var text = AdvertisedAddress.Trim();
        if (IPAddress.TryParse(text, out var parsed) && parsed.AddressFamily == AddressFamily.InterNetwork && parsed.ToString() == text)
        {
            address = parsed;
            return true;
        }

        return false;
    }
}
