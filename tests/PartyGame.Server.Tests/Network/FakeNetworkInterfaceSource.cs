using PartyGame.Server.Network;

namespace PartyGame.Server.Tests.Network;

internal sealed class FakeNetworkInterfaceSource(params NetworkInterfaceInfo[] interfaces) : INetworkInterfaceSource
{
    public IReadOnlyList<NetworkInterfaceInfo> GetInterfaces() => interfaces;
}
