using System.Net;
using PartyGame.Server.Network;
using static PartyGame.Server.Tests.Network.TestInterfaces;

namespace PartyGame.Server.Tests.Network;

public sealed class StartupBannerTests
{
    [Fact]
    public void Format_DetectedAddress_GivesScreenUrls()
    {
        var banner = StartupBanner.Format(AddressSelector.Select([Wifi("192.168.1.42")], configured: null), port: 5000);

        Assert.Contains("192.168.1.42 (détectée)", banner, StringComparison.Ordinal);
        Assert.Contains("http://192.168.1.42:5000/display/", banner, StringComparison.Ordinal);
        Assert.Contains("http://192.168.1.42:5000/gm/", banner, StringComparison.Ordinal);
        Assert.DoesNotContain("Autres adresses", banner, StringComparison.Ordinal);
    }

    [Fact]
    public void Format_SeveralCandidates_ListsOthersAndHowToImposeOne()
    {
        var selection = AddressSelector.Select([Wifi("192.168.1.42"), Ethernet("10.0.0.2", hasGateway: false, name: "Ethernet 2")], configured: null);

        var banner = StartupBanner.Format(selection, port: 5001);

        Assert.Contains("http://192.168.1.42:5001/display/", banner, StringComparison.Ordinal);
        Assert.Contains("10.0.0.2 (Ethernet 2)", banner, StringComparison.Ordinal);
        Assert.Contains("--Network:AdvertisedAddress=10.0.0.2", banner, StringComparison.Ordinal);
        Assert.Contains("Network__AdvertisedAddress=10.0.0.2", banner, StringComparison.Ordinal);
    }

    [Fact]
    public void Format_ConfiguredAddressOnNoInterface_SaysWhereItComesFromAndWarns()
    {
        var selection = AddressSelector.Select([Wifi("192.168.1.42")], IPAddress.Parse("192.168.50.7"));

        var banner = StartupBanner.Format(selection, port: 5000);

        Assert.Contains("192.168.50.7 (imposée par Network:AdvertisedAddress)", banner, StringComparison.Ordinal);
        Assert.Contains("aucune interface réseau active", banner, StringComparison.Ordinal);
        Assert.Contains("http://192.168.50.7:5000/display/", banner, StringComparison.Ordinal);
        Assert.Contains("192.168.1.42 (Wi-Fi)", banner, StringComparison.Ordinal);
    }

    [Fact]
    public void Format_NoAddress_AsksToConnectAndGivesLocalUrls()
    {
        var banner = StartupBanner.Format(AddressSelector.Select([], configured: null), port: 5000);

        Assert.Contains("Connectez ce PC au Wi-Fi", banner, StringComparison.Ordinal);
        Assert.Contains("http://localhost:5000/display/", banner, StringComparison.Ordinal);
        Assert.Contains("http://localhost:5000/gm/", banner, StringComparison.Ordinal);
    }
}
