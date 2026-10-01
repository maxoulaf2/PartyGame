namespace PartyGame.Server.Network;

internal sealed class NetworkOptions
{
    public const string SectionName = "Network";

    public const int DefaultPort = 5000;

    // HTTP port listened to on every IPv4 interface. Other services (startup banner, QR code)
    // read it from here rather than parsing the listening URLs.
    public int Port { get; init; } = DefaultPort;

    public static NetworkOptions Read(IConfiguration configuration) =>
        configuration.GetSection(SectionName).Get<NetworkOptions>() ?? new NetworkOptions();
}
