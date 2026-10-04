using System.Net.NetworkInformation;

namespace PartyGame.Server.Network;

/// <summary>What the address selection needs to know about a network interface of the host.</summary>
internal sealed record NetworkInterfaceInfo(
    string Name,
    string Description,
    NetworkInterfaceType Type,
    bool IsUp,
    IReadOnlyList<InterfaceAddress> IPv4Addresses,
    bool HasGateway);
