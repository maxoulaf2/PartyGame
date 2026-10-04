using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PartyGame.Server.Network;

namespace PartyGame.Server.Tests;

public sealed class NetworkListeningTests : IDisposable
{
    private static readonly TimeSpan _startupTimeout = TimeSpan.FromSeconds(30);

    private readonly TempDirectory _logs = new();

    public void Dispose() => _logs.Dispose();

    [Fact]
    public async Task Configuration_Default_ListensOnPort5000()
    {
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseScratchDirectory(_logs.Path));

        var options = factory.Services.GetRequiredService<IOptions<NetworkOptions>>().Value;

        Assert.Equal(5000, options.Port);
    }

    [Fact]
    public async Task Startup_PortSetByEnvironment_AnswersOnLoopbackAndLocalNetworkAddresses()
    {
        var localAddress = ServerProcess.FindNonLoopbackIPv4Address();
        Assert.SkipWhen(localAddress is null, "This machine has no active non-loopback IPv4 address");
        var port = ServerProcess.GetFreePort();
        using var server = ServerProcess.Start(ServerEnvironment(port));
        using var client = new HttpClient();

        using var loopback = await server.WaitForResponseAsync(client, new Uri($"http://localhost:{port}/health"), _startupTimeout);
        using var network = await client.GetAsync(new Uri($"http://{localAddress}:{port}/health"), TestContext.Current.CancellationToken);

        // The console output is read asynchronously: it may lag behind the first response.
        var output = await server.WaitForOutputAsync($"http://0.0.0.0:{port}", _startupTimeout);

        Assert.Equal(HttpStatusCode.OK, loopback.StatusCode);
        Assert.Equal(HttpStatusCode.OK, network.StatusCode);
        Assert.Contains($"http://0.0.0.0:{port}", output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Startup_ServerReady_PrintsBannerWithScreenUrlsToConsole()
    {
        var port = ServerProcess.GetFreePort();
        var environment = ServerEnvironment(port);
        environment["Network__AdvertisedAddress"] = "192.168.50.7";
        using var server = ServerProcess.Start(environment);
        using var client = new HttpClient();

        using var response = await server.WaitForResponseAsync(client, new Uri($"http://localhost:{port}/health"), _startupTimeout);
        var output = await server.WaitForOutputAsync("/gm/", _startupTimeout);

        Assert.Contains($"http://192.168.50.7:{port}/display/", output, StringComparison.Ordinal);
        Assert.Contains($"http://192.168.50.7:{port}/gm/", output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Startup_InvalidAdvertisedAddress_ExitsNamingTheSetting()
    {
        var environment = ServerEnvironment(ServerProcess.GetFreePort());
        environment["Network__AdvertisedAddress"] = "partygame.local";
        using var server = ServerProcess.Start(environment);

        var exitCode = await server.WaitForExitAsync(_startupTimeout);

        Assert.Equal(1, exitCode);
        Assert.Contains("Network:AdvertisedAddress must be an IPv4 address", server.Output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Startup_PortAlreadyInUse_ExitsWithPlainFatalMessage()
    {
        using var occupant = new TcpListener(IPAddress.Any, 0);
        occupant.Start();
        var port = ((IPEndPoint)occupant.LocalEndpoint).Port;
        using var server = ServerProcess.Start(ServerEnvironment(port));

        var exitCode = await server.WaitForExitAsync(_startupTimeout);

        Assert.Equal(1, exitCode);
        Assert.Contains($"FTL] Port {port} is already in use", server.Output, StringComparison.Ordinal);
        Assert.Contains("Network:Port", server.Output, StringComparison.Ordinal);
        Assert.DoesNotContain("   at ", server.Output, StringComparison.Ordinal);
    }

    private Dictionary<string, string> ServerEnvironment(int port) => new()
    {
        ["Network__Port"] = port.ToString(System.Globalization.CultureInfo.InvariantCulture),
        ["LogFiles__Directory"] = _logs.Path,
        ["Persistence__Directory"] = _logs.Path,
    };
}
