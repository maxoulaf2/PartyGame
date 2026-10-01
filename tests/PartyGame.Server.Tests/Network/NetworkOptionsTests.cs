using System.Net;
using PartyGame.Server.Network;

namespace PartyGame.Server.Tests.Network;

public sealed class NetworkOptionsTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void TryGetAdvertisedAddress_NotSet_SucceedsWithoutAddress(string? value)
    {
        var options = new NetworkOptions { AdvertisedAddress = value };

        Assert.True(options.TryGetAdvertisedAddress(out var address));
        Assert.Null(address);
    }

    [Theory]
    [InlineData("192.168.1.42")]
    [InlineData(" 192.168.1.42 ")]
    public void TryGetAdvertisedAddress_DottedIPv4_ReturnsIt(string value)
    {
        var options = new NetworkOptions { AdvertisedAddress = value };

        Assert.True(options.TryGetAdvertisedAddress(out var address));
        Assert.Equal(IPAddress.Parse("192.168.1.42"), address);
    }

    [Theory]
    [InlineData("10.1")]
    [InlineData("192.168.001.042")]
    [InlineData("fe80::1")]
    [InlineData("partygame.local")]
    [InlineData("192.168.1.256")]
    public void TryGetAdvertisedAddress_NotADottedIPv4_Fails(string value)
    {
        var options = new NetworkOptions { AdvertisedAddress = value };

        Assert.False(options.TryGetAdvertisedAddress(out _));
    }
}
