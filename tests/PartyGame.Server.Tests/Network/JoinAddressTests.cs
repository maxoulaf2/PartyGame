using System.Net;
using System.Net.NetworkInformation;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using PartyGame.Contracts;
using PartyGame.Server.Hubs;
using PartyGame.Server.Network;
using PartyGame.Server.Tests.Hubs;
using static PartyGame.Server.Tests.Network.TestInterfaces;

namespace PartyGame.Server.Tests.Network;

public sealed class JoinAddressTests : IDisposable
{
    private readonly TempDirectory _logs = new();

    public void Dispose() => _logs.Dispose();

    [Fact]
    public async Task DisplaySnapshot_SeveralInterfaces_CarriesOnlyTheAdvertisedAddress()
    {
        await using var factory = CreateFactory(
            new FakeNetworkInterfaceSource(
                Wifi("192.168.1.42"),
                Ethernet("10.0.0.2", hasGateway: false),
                Nic("vEthernet (WSL)", "Hyper-V Virtual Ethernet Adapter", NetworkInterfaceType.Ethernet, "172.29.0.1")));

        var (snapshot, json) = await ReceiveDisplaySnapshotAsync(factory);

        Assert.Equal("192.168.1.42", snapshot.JoinAddress);
        // The other candidates and the names of the interfaces stay on the operator console.
        Assert.DoesNotContain("10.0.0.2", json, StringComparison.Ordinal);
        Assert.DoesNotContain("172.29.0.1", json, StringComparison.Ordinal);
        Assert.DoesNotContain("Wi-Fi", json, StringComparison.Ordinal);
        Assert.DoesNotContain("Ethernet", json, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DisplaySnapshot_NoPrivateAddress_CarriesNoAddressAndLogsWarning()
    {
        await using var factory = CreateFactory(new FakeNetworkInterfaceSource(Nic("lo", "lo", NetworkInterfaceType.Loopback, "127.0.0.1")));

        var (snapshot, _) = await ReceiveDisplaySnapshotAsync(factory);

        Assert.Null(snapshot.JoinAddress);
        Assert.Contains("No private IPv4 address found", _logs.ReadAllLogs(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task DisplaySnapshot_AddressConfigured_CarriesItEvenOffInterfaceAndLogsWarning()
    {
        await using var factory = CreateFactory(new FakeNetworkInterfaceSource(Wifi("192.168.1.42")), advertisedAddress: "192.168.50.7");

        var (snapshot, _) = await ReceiveDisplaySnapshotAsync(factory);

        Assert.Equal("192.168.50.7", snapshot.JoinAddress);
        Assert.Contains("belongs to no active network interface", _logs.ReadAllLogs(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetJoin_EndpointRemoved_IsNotFound()
    {
        await using var factory = CreateFactory(new FakeNetworkInterfaceSource(Wifi("192.168.1.42")));
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/api/join", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
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

    private static async Task<(DisplaySnapshot Snapshot, string Json)> ReceiveDisplaySnapshotAsync(WebApplicationFactory<Program> factory)
    {
        await using var connection = await HubClients.ConnectAsync(factory);
        using var received = new ReceivedSnapshots(connection);

        await connection.InvokeAsync<AnnouncementResult>(GameHub.Announce, new Announcement(Role.Display, null), TestContext.Current.CancellationToken);
        await HubClients.FlushAsync<GameHub>(factory, connection);

        return (Assert.Single(received.Display), Assert.Single(received.Json));
    }
}
