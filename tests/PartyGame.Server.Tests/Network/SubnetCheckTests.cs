using System.Net;
using PartyGame.Server.Network;
using static PartyGame.Server.Tests.Network.TestInterfaces;

namespace PartyGame.Server.Tests.Network;

public sealed class SubnetCheckTests
{
    private static readonly NetworkInterfaceInfo[] _host = [Wifi("192.168.1.42"), Ethernet("10.0.0.2", hasGateway: false)];

    [Theory]
    [InlineData("192.168.1.42", "192.168.1.17", true)]
    [InlineData("192.168.1.42", "192.168.1.255", true)]
    [InlineData("192.168.1.42", "192.168.2.17", false)]
    [InlineData("192.168.1.42", "10.0.0.7", false)]
    [InlineData("10.0.0.2", "10.0.0.7", true)]
    public void IsOnAdvertisedNetwork_DeviceAddress_ComparesTheNetworksOfThePrefix(string advertised, string device, bool expected) =>
        Assert.Equal(expected, SubnetCheck.IsOnAdvertisedNetwork(_host, advertised, IPAddress.Parse(device)));

    [Fact]
    public void IsOnAdvertisedNetwork_ShortPrefix_CoversTheWholeNetwork()
    {
        NetworkInterfaceInfo[] host = [Wifi("172.16.5.1") with { IPv4Addresses = [new InterfaceAddress(IPAddress.Parse("172.16.5.1"), 16)] }];

        Assert.True(SubnetCheck.IsOnAdvertisedNetwork(host, "172.16.5.1", IPAddress.Parse("172.16.200.9")));
    }

    [Fact]
    public void IsOnAdvertisedNetwork_IPv4MappedDevice_ComparesItsIPv4Address() =>
        Assert.True(SubnetCheck.IsOnAdvertisedNetwork(_host, "192.168.1.42", IPAddress.Parse("::ffff:192.168.1.17")));

    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("::1")]
    public void IsOnAdvertisedNetwork_HostItself_IsOnTheNetwork(string device) =>
        Assert.True(SubnetCheck.IsOnAdvertisedNetwork(_host, "192.168.1.42", IPAddress.Parse(device)));

    [Theory]
    [InlineData(null, "192.168.1.17")]
    [InlineData("192.168.50.7", "192.168.50.8")]
    [InlineData("192.168.1.42", "fe80::1")]
    [InlineData("192.168.1.42", null)]
    public void IsOnAdvertisedNetwork_UnknownNetworkOrDevice_CannotTell(string? advertised, string? device) =>
        Assert.Null(SubnetCheck.IsOnAdvertisedNetwork(_host, advertised, device is null ? null : IPAddress.Parse(device)));
}
