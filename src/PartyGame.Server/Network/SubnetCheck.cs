using System.Net;
using System.Net.Sockets;

namespace PartyGame.Server.Network;

/// <summary>
/// Tells whether a device belongs to the network of the address advertised to phones. Pure, so that every network layout
/// can be tested without touching the real network.
/// </summary>
internal static class SubnetCheck
{
    /// <param name="interfaces">The interfaces of the host, which give the length of the prefix of the advertised address.</param>
    /// <param name="advertised">The address advertised to phones, if any.</param>
    /// <param name="device">The address the connection of the device comes from, if known.</param>
    /// <returns>
    /// <see langword="null"/> when it cannot tell: no advertised address, no interface of the host holding it, or no IPv4
    /// address for the device.
    /// </returns>
    public static bool? IsOnAdvertisedNetwork(IReadOnlyList<NetworkInterfaceInfo> interfaces, string? advertised, IPAddress? device)
    {
        if (device is null || !IPAddress.TryParse(advertised, out var address))
        {
            return null;
        }

        if (device.IsIPv4MappedToIPv6)
        {
            device = device.MapToIPv4();
        }

        if (IPAddress.IsLoopback(device))
        {
            // The host itself, such as the PC of the server opening the page at its local address, or the Vite dev server.
            return true;
        }

        var network = interfaces.SelectMany(nic => nic.IPv4Addresses).FirstOrDefault(a => a.Address.Equals(address));
        if (network is null || device.AddressFamily != AddressFamily.InterNetwork)
        {
            return null;
        }

        var mask = network.PrefixLength == 0 ? 0u : uint.MaxValue << (32 - network.PrefixLength);
        return (AddressSelector.ToNumber(device) & mask) == (AddressSelector.ToNumber(address) & mask);
    }
}
