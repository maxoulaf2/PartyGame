using System.Net;
using System.Net.NetworkInformation;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using PartyGame.Server.Network;
using static PartyGame.Server.Tests.Network.TestInterfaces;

namespace PartyGame.Server.Tests.Network;

public sealed class JoinInfoEndpointTests : IDisposable
{
    private readonly TempDirectory _logs = new();

    public void Dispose() => _logs.Dispose();

    [Fact]
    public async Task GetJoin_SeveralInterfaces_ReturnsOnlyTheAdvertisedAddress()
    {
        await using var factory = CreateFactory(
            new FakeNetworkInterfaceSource(
                Wifi("192.168.1.42"),
                Ethernet("10.0.0.2", hasGateway: false),
                Nic("vEthernet (WSL)", "Hyper-V Virtual Ethernet Adapter", NetworkInterfaceType.Ethernet, "172.29.0.1")));

        using var json = await GetJoinAsync(factory);

        var property = Assert.Single(json.RootElement.EnumerateObject());
        Assert.Equal("address", property.Name);
        Assert.Equal("192.168.1.42", property.Value.GetString());
    }

    [Fact]
    public async Task GetJoin_NoPrivateAddress_ReturnsNullAddressAndLogsWarning()
    {
        await using var factory = CreateFactory(new FakeNetworkInterfaceSource(Nic("lo", "lo", NetworkInterfaceType.Loopback, "127.0.0.1")));

        using var json = await GetJoinAsync(factory);

        Assert.Equal(JsonValueKind.Null, json.RootElement.GetProperty("address").ValueKind);
        Assert.Contains("No private IPv4 address found", _logs.ReadAllLogs(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetJoin_AddressConfigured_ReturnsItEvenOffInterfaceAndLogsWarning()
    {
        await using var factory = CreateFactory(new FakeNetworkInterfaceSource(Wifi("192.168.1.42")), advertisedAddress: "192.168.50.7");

        using var json = await GetJoinAsync(factory);

        Assert.Equal("192.168.50.7", json.RootElement.GetProperty("address").GetString());
        Assert.Contains("belongs to no active network interface", _logs.ReadAllLogs(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetJoin_Answered_IsNeverCached()
    {
        await using var factory = CreateFactory(new FakeNetworkInterfaceSource(Wifi("192.168.1.42")));
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/api/join", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.True(response.Headers.CacheControl?.NoStore);
    }

    [Fact]
    public async Task Startup_RealHost_RegistersSystemInterfaceSource()
    {
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseSetting("LogFiles:Directory", _logs.Path));

        Assert.IsType<SystemNetworkInterfaceSource>(factory.Services.GetRequiredService<INetworkInterfaceSource>());
    }

    private WebApplicationFactory<Program> CreateFactory(INetworkInterfaceSource source, string? advertisedAddress = null) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LogFiles:Directory", _logs.Path);
            if (advertisedAddress is not null)
            {
                builder.UseSetting("Network:AdvertisedAddress", advertisedAddress);
            }

            builder.ConfigureTestServices(services => services.AddSingleton(source));
        });

    private static async Task<JsonDocument> GetJoinAsync(WebApplicationFactory<Program> factory)
    {
        using var client = factory.CreateClient();
        var body = await client.GetStringAsync(new Uri("/api/join", UriKind.Relative), TestContext.Current.CancellationToken);
        return JsonDocument.Parse(body);
    }
}
