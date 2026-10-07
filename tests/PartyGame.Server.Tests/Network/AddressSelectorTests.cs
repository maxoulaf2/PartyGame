using System.Net;
using System.Net.NetworkInformation;
using PartyGame.Engine.State;
using PartyGame.Server.Network;
using static PartyGame.Server.Tests.Network.TestInterfaces;

namespace PartyGame.Server.Tests.Network;

public sealed class AddressSelectorTests
{
    [Fact]
    public void Select_SingleWifi_AdvertisesItsAddress()
    {
        var selection = AddressSelector.Select([Wifi("192.168.1.42")], configured: null);

        Assert.Equal(IPAddress.Parse("192.168.1.42"), selection.Address);
        Assert.False(selection.IsConfigured);
        Assert.True(selection.IsOnActiveInterface);
        Assert.Empty(selection.OtherCandidates);
    }

    [Fact]
    public void Select_WifiWithHyperVWslAndVpn_IgnoresVirtualInterfaces()
    {
        NetworkInterfaceInfo[] interfaces =
        [
            Nic("vEthernet (Default Switch)", "Hyper-V Virtual Ethernet Adapter", NetworkInterfaceType.Ethernet, "172.20.16.1"),
            Nic("vEthernet (WSL (Hyper-V firewall))", "Hyper-V Virtual Ethernet Adapter #2", NetworkInterfaceType.Ethernet, "172.29.0.1"),
            Nic("Tailscale", "Tailscale Tunnel", NetworkInterfaceType.Unknown, "100.101.102.103"),
            Nic("OpenVPN Connect", "TAP-Windows Adapter V9", NetworkInterfaceType.Ethernet, "10.8.0.6", hasGateway: true),
            Nic("Corporate VPN", "WAN Miniport (PPTP)", NetworkInterfaceType.Ppp, "10.20.0.3", hasGateway: true),
            Wifi("192.168.1.42"),
        ];

        var selection = AddressSelector.Select(interfaces, configured: null);

        Assert.Equal(IPAddress.Parse("192.168.1.42"), selection.Address);
        Assert.Single(selection.Candidates);
    }

    [Theory]
    [InlineData("VirtualBox Host-Only Network", "VirtualBox Host-Only Ethernet Adapter", "192.168.56.1")]
    [InlineData("VMware Network Adapter VMnet8", "VMware Virtual Ethernet Adapter for VMnet8", "192.168.204.1")]
    [InlineData("Ethernet 3", "Docker Desktop Virtual Adapter", "10.0.75.1")]
    [InlineData("WireGuard", "WireGuard Tunnel", "10.6.0.2")]
    [InlineData("ZeroTier One [8056c2e21c000001]", "ZeroTier Virtual Port", "10.147.17.5")]
    [InlineData("docker0", "docker0", "172.17.0.1")]
    [InlineData("br-3f2a9c1d7e4b", "br-3f2a9c1d7e4b", "172.18.0.1")]
    [InlineData("veth1a2b3c4", "veth1a2b3c4", "172.17.0.2")]
    [InlineData("virbr0", "virbr0", "192.168.122.1")]
    [InlineData("wg0", "wg0", "10.6.0.3")]
    [InlineData("tun0", "tun0", "10.8.0.10")]
    public void Select_VirtualInterface_IsNotACandidate(string name, string description, string address)
    {
        var selection = AddressSelector.Select([Nic(name, description, NetworkInterfaceType.Ethernet, address)], configured: null);

        Assert.Null(selection.Address);
        Assert.Empty(selection.Candidates);
    }

    [Fact]
    public void Select_RaspberryPiWithDocker_AdvertisesWlan()
    {
        NetworkInterfaceInfo[] interfaces =
        [
            Nic("lo", "lo", NetworkInterfaceType.Loopback, "127.0.0.1"),
            Nic("eth0", "eth0", NetworkInterfaceType.Ethernet, "192.168.1.60", isUp: false),
            Nic("wlan0", "wlan0", NetworkInterfaceType.Wireless80211, "192.168.1.61", hasGateway: true),
            Nic("docker0", "docker0", NetworkInterfaceType.Ethernet, "172.17.0.1"),
        ];

        var selection = AddressSelector.Select(interfaces, configured: null);

        Assert.Equal(IPAddress.Parse("192.168.1.61"), selection.Address);
    }

    [Theory]
    [InlineData("127.0.0.1", NetworkInterfaceType.Loopback)]
    [InlineData("169.254.12.34", NetworkInterfaceType.Ethernet)]
    [InlineData("100.72.1.2", NetworkInterfaceType.Ethernet)]
    [InlineData("82.64.10.20", NetworkInterfaceType.Ethernet)]
    [InlineData("10.0.0.5", NetworkInterfaceType.Tunnel)]
    public void Select_NonLocalNetworkAddress_IsNotACandidate(string address, NetworkInterfaceType type)
    {
        var selection = AddressSelector.Select([Nic("Ethernet", "Adapter", type, address, hasGateway: true)], configured: null);

        Assert.Null(selection.Address);
    }

    [Fact]
    public void Select_InactiveInterface_IsNotACandidate()
    {
        var selection = AddressSelector.Select(
            [Nic("Wi-Fi", "Intel(R) Wi-Fi 6", NetworkInterfaceType.Wireless80211, "192.168.1.42", hasGateway: true, isUp: false)],
            configured: null);

        Assert.Null(selection.Address);
    }

    [Fact]
    public void Select_NoInterface_AdvertisesNothing()
    {
        var selection = AddressSelector.Select([], configured: null);

        Assert.Null(selection.Address);
        Assert.False(selection.IsOnActiveInterface);
        Assert.Empty(selection.Candidates);
    }

    [Fact]
    public void Select_OnlyOneInterfaceHasGateway_PrefersIt()
    {
        var selection = AddressSelector.Select(
            [Ethernet("10.0.0.2", hasGateway: false), Wifi("192.168.1.42", hasGateway: true)],
            configured: null);

        Assert.Equal(IPAddress.Parse("192.168.1.42"), selection.Address);
        Assert.Equal([IPAddress.Parse("10.0.0.2")], selection.OtherCandidates.Select(candidate => candidate.Address));
    }

    [Fact]
    public void Select_EthernetAndWifiBothWithGateway_ChoiceDoesNotDependOnInterfaceOrder()
    {
        var ethernet = Ethernet("192.168.1.20");
        var wifi = Wifi("192.168.1.42");

        var first = AddressSelector.Select([ethernet, wifi], configured: null);
        var second = AddressSelector.Select([wifi, ethernet], configured: null);

        Assert.Equal(IPAddress.Parse("192.168.1.20"), first.Address);
        Assert.Equal(first.Address, second.Address);
        Assert.Equal(first.Candidates, second.Candidates);
    }

    [Fact]
    public void Select_AddressesCompared_UsesNumericOrderNotText()
    {
        var selection = AddressSelector.Select([Ethernet("192.168.1.100"), Wifi("192.168.1.9")], configured: null);

        Assert.Equal(IPAddress.Parse("192.168.1.9"), selection.Address);
    }

    [Fact]
    public void Select_ConfiguredAddressAmongCandidates_IsAdvertisedAndOthersListed()
    {
        var selection = AddressSelector.Select(
            [Ethernet("192.168.1.20"), Wifi("192.168.1.42")],
            configured: IPAddress.Parse("192.168.1.42"));

        Assert.Equal(IPAddress.Parse("192.168.1.42"), selection.Address);
        Assert.True(selection.IsConfigured);
        Assert.True(selection.IsOnActiveInterface);
        Assert.Equal([IPAddress.Parse("192.168.1.20")], selection.OtherCandidates.Select(candidate => candidate.Address));
    }

    [Fact]
    public void Select_ConfiguredAddressOnVirtualInterface_IsAdvertisedAsIs()
    {
        var selection = AddressSelector.Select(
            [Wifi("192.168.1.42"), Nic("vEthernet (WSL)", "Hyper-V Virtual Ethernet Adapter", NetworkInterfaceType.Ethernet, "172.29.0.1")],
            configured: IPAddress.Parse("172.29.0.1"));

        Assert.Equal(IPAddress.Parse("172.29.0.1"), selection.Address);
        Assert.True(selection.IsOnActiveInterface);
    }

    [Fact]
    public void Select_ConfiguredAddressOnNoInterface_IsAdvertisedButFlagged()
    {
        var selection = AddressSelector.Select([Wifi("192.168.1.42")], configured: IPAddress.Parse("192.168.50.7"));

        Assert.Equal(IPAddress.Parse("192.168.50.7"), selection.Address);
        Assert.True(selection.IsConfigured);
        Assert.False(selection.IsOnActiveInterface);
    }

    [Theory]
    [InlineData("10.0.0.1", true)]
    [InlineData("10.255.255.254", true)]
    [InlineData("172.16.0.1", true)]
    [InlineData("172.31.255.1", true)]
    [InlineData("192.168.0.1", true)]
    [InlineData("172.15.0.1", false)]
    [InlineData("172.32.0.1", false)]
    [InlineData("192.169.0.1", false)]
    [InlineData("169.254.1.1", false)]
    [InlineData("100.64.0.1", false)]
    [InlineData("8.8.8.8", false)]
    [InlineData("::1", false)]
    public void IsPrivate_Address_MatchesRfc1918Ranges(string address, bool expected)
    {
        Assert.Equal(expected, AddressSelector.IsPrivate(IPAddress.Parse(address)));
    }

    [Fact]
    public void ToJoinAddressCandidates_Detected_ListsEveryCandidateBestFirstWithItsInterface()
    {
        var selection = AddressSelector.Select([Ethernet("10.0.0.2", hasGateway: false), Wifi("192.168.1.42")], configured: null);

        Assert.Equal(
            [new JoinAddressCandidate("192.168.1.42", "Wi-Fi"), new JoinAddressCandidate("10.0.0.2", "Ethernet")],
            selection.ToJoinAddressCandidates());
    }

    [Fact]
    public void ToJoinAddressCandidates_ConfiguredOffCandidates_PutsItFirstWithoutInterface()
    {
        var selection = AddressSelector.Select([Wifi("192.168.1.42")], configured: IPAddress.Parse("192.168.50.7"));

        Assert.Equal(
            [new JoinAddressCandidate("192.168.50.7", InterfaceName: null), new JoinAddressCandidate("192.168.1.42", "Wi-Fi")],
            selection.ToJoinAddressCandidates());
    }

    [Fact]
    public void ToJoinAddressCandidates_ConfiguredAmongCandidates_ListsItOnceWithItsInterface()
    {
        var selection = AddressSelector.Select(
            [Wifi("192.168.1.42"), Ethernet("10.0.0.2", hasGateway: false)],
            configured: IPAddress.Parse("10.0.0.2"));

        Assert.Equal(
            [new JoinAddressCandidate("192.168.1.42", "Wi-Fi"), new JoinAddressCandidate("10.0.0.2", "Ethernet")],
            selection.ToJoinAddressCandidates());
    }

    [Fact]
    public void ToJoinAddressCandidates_NoAddress_IsEmpty()
    {
        var selection = AddressSelector.Select([], configured: null);

        Assert.Empty(selection.ToJoinAddressCandidates());
    }
}
