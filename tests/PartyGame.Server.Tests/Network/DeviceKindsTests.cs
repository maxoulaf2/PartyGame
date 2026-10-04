using PartyGame.Contracts;
using PartyGame.Server.Network;

namespace PartyGame.Server.Tests.Network;

public sealed class DeviceKindsTests
{
    [Theory]
    [InlineData("Mozilla/5.0 (iPhone; CPU iPhone OS 17_5 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.5 Mobile/15E148 Safari/604.1", DeviceKind.IPhone)]
    [InlineData("Mozilla/5.0 (iPad; CPU OS 16_6 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/16.6 Mobile/15E148 Safari/604.1", DeviceKind.IPad)]
    [InlineData("Mozilla/5.0 (Linux; Android 14; Pixel 7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/129.0.0.0 Mobile Safari/537.36", DeviceKind.Android)]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/129.0.0.0 Safari/537.36", DeviceKind.Windows)]
    [InlineData("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.5 Safari/605.1.15", DeviceKind.Mac)]
    [InlineData("Mozilla/5.0 (X11; Linux aarch64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/129.0.0.0 Safari/537.36", DeviceKind.Linux)]
    [InlineData("curl/8.4.0", DeviceKind.Other)]
    [InlineData(null, DeviceKind.Other)]
    public void FromUserAgent_Browser_TellsTheKindOfDevice(string? userAgent, DeviceKind expected) =>
        Assert.Equal(expected, DeviceKinds.FromUserAgent(userAgent));
}
