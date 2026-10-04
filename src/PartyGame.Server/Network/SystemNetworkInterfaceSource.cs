using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace PartyGame.Server.Network;

internal sealed class SystemNetworkInterfaceSource : INetworkInterfaceSource
{
    public IReadOnlyList<NetworkInterfaceInfo> GetInterfaces() =>
        [.. NetworkInterface.GetAllNetworkInterfaces().Select(Describe)];

    private static NetworkInterfaceInfo Describe(NetworkInterface nic)
    {
        var properties = nic.GetIPProperties();
        return new NetworkInterfaceInfo(
            nic.Name,
            nic.Description,
            nic.NetworkInterfaceType,
            nic.OperationalStatus == OperationalStatus.Up,
            [.. properties.UnicastAddresses.Where(unicast => IsIPv4(unicast.Address)).Select(unicast => new InterfaceAddress(unicast.Address, unicast.PrefixLength))],
            properties.GatewayAddresses.Any(gateway => IsIPv4(gateway.Address) && !gateway.Address.Equals(IPAddress.Any)));
    }

    private static bool IsIPv4(IPAddress address) => address.AddressFamily == AddressFamily.InterNetwork;
}
