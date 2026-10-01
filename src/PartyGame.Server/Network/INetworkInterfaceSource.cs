namespace PartyGame.Server.Network;

/// <summary>Lists the network interfaces of the host, so that the address selection can be tested without a network.</summary>
internal interface INetworkInterfaceSource
{
    IReadOnlyList<NetworkInterfaceInfo> GetInterfaces();
}
