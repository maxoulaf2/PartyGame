using System.Net;

namespace PartyGame.Server.Network;

internal static partial class NetworkLog
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Advertising address {Address} to phones (configured: {IsConfigured})")]
    public static partial void AddressAdvertised(this ILogger logger, IPAddress address, bool isConfigured);

    [LoggerMessage(Level = LogLevel.Warning, Message = "No private IPv4 address found: connect this computer to the Wi-Fi of the venue, then restart the server")]
    public static partial void NoPrivateAddress(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Configured address {Address} ({Setting}) belongs to no active network interface: phones may not reach the server")]
    public static partial void ConfiguredAddressNotOnInterface(this ILogger logger, IPAddress address, string setting);
}
