using System.Net;
using System.Net.NetworkInformation;
using PartyGame.Server.Network;

namespace PartyGame.Server.Tests.Network;

/// <summary>Interfaces as reported by real Windows and Linux hosts, to describe network layouts in tests.</summary>
internal static class TestInterfaces
{
    public static NetworkInterfaceInfo Wifi(string address, bool hasGateway = true) =>
        Nic("Wi-Fi", "Intel(R) Wi-Fi 6 AX201 160MHz", NetworkInterfaceType.Wireless80211, address, hasGateway);

    public static NetworkInterfaceInfo Ethernet(string address, bool hasGateway = true, string name = "Ethernet") =>
        Nic(name, "Realtek PCIe GbE Family Controller", NetworkInterfaceType.Ethernet, address, hasGateway);

    public static NetworkInterfaceInfo Nic(
        string name,
        string description,
        NetworkInterfaceType type,
        string address,
        bool hasGateway = false,
        bool isUp = true) =>
        new(name, description, type, isUp, [new InterfaceAddress(IPAddress.Parse(address), PrefixLength: 24)], hasGateway);
}
