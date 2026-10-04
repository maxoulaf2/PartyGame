using System.Buffers.Binary;
using System.Net;
using System.Net.NetworkInformation;

namespace PartyGame.Server.Network;

/// <summary>
/// Chooses the address phones use to reach the server, from the interfaces of the host. Pure, so that every
/// network layout can be tested without touching the real network.
/// </summary>
internal static class AddressSelector
{
    // VPNs and dial-up links do not lead to the phones of the room.
    private static readonly NetworkInterfaceType[] _excludedTypes =
        [NetworkInterfaceType.Loopback, NetworkInterfaceType.Tunnel, NetworkInterfaceType.Ppp];

    // Virtual adapters (hypervisors, containers, VPN clients) often report themselves as plain Ethernet,
    // so their type is not enough: their Windows name or description gives them away.
    private static readonly string[] _virtualMarkers =
    [
        "vEthernet", "Hyper-V", "WSL", "Docker", "VirtualBox", "VMware", "VMnet",
        "TAP-Windows", "Wintun", "WireGuard", "Tailscale", "ZeroTier", "OpenVPN",
    ];

    // Linux names of the same adapters, as found on a Raspberry Pi.
    private static readonly string[] _virtualNamePrefixes =
        ["docker", "br-", "veth", "virbr", "vboxnet", "vmnet", "tailscale", "zt", "wg", "tun", "tap"];

    /// <param name="interfaces">The interfaces of the host.</param>
    /// <param name="configured">The address imposed by <c>Network:AdvertisedAddress</c>, if any: it always wins.</param>
    public static AddressSelection Select(IReadOnlyList<NetworkInterfaceInfo> interfaces, IPAddress? configured)
    {
        var candidates = interfaces
            .Where(nic => nic.IsUp && !_excludedTypes.Contains(nic.Type) && !IsVirtual(nic))
            .SelectMany(nic => nic.IPv4Addresses.Select(a => a.Address).Where(IsPrivate).Select(address => new AddressCandidate(address, nic.Name, nic.HasGateway)))
            // An interface with a default gateway is the one connected to the router the phones use. The rest
            // of the order only makes the choice stable from one startup to the next.
            .OrderByDescending(candidate => candidate.HasGateway)
            .ThenBy(candidate => ToNumber(candidate.Address))
            .ThenBy(candidate => candidate.InterfaceName, StringComparer.Ordinal)
            .ToList();

        if (configured is not null)
        {
            var isOnActiveInterface = interfaces.Any(nic => nic.IsUp && nic.IPv4Addresses.Any(a => a.Address.Equals(configured)));
            return new AddressSelection(configured, IsConfigured: true, candidates, isOnActiveInterface);
        }

        var best = candidates.FirstOrDefault();
        return new AddressSelection(best?.Address, IsConfigured: false, candidates, IsOnActiveInterface: best is not null);
    }

    // RFC 1918 ranges only: link-local (169.254.0.0/16) means no DHCP answer, and 100.64.0.0/10 is used
    // by carrier-grade NAT and mesh VPNs, none of which phones on the venue Wi-Fi can reach.
    public static bool IsPrivate(IPAddress address)
    {
        var bytes = address.GetAddressBytes();
        return bytes.Length == 4
            && (bytes[0] == 10
                || (bytes[0] == 172 && bytes[1] is >= 16 and <= 31)
                || (bytes[0] == 192 && bytes[1] == 168));
    }

    private static bool IsVirtual(NetworkInterfaceInfo nic) =>
        _virtualMarkers.Any(marker =>
            nic.Name.Contains(marker, StringComparison.OrdinalIgnoreCase)
            || nic.Description.Contains(marker, StringComparison.OrdinalIgnoreCase))
        || _virtualNamePrefixes.Any(prefix => nic.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

    public static uint ToNumber(IPAddress address) => BinaryPrimitives.ReadUInt32BigEndian(address.GetAddressBytes());
}
