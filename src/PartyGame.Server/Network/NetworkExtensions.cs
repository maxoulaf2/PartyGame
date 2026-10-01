using System.Net;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Options;

namespace PartyGame.Server.Network;

internal static class NetworkExtensions
{
    /// <summary>
    /// Listens over HTTP on every IPv4 interface, so that phones on the local network reach the server,
    /// on the port given by <see cref="NetworkOptions"/>. This replaces any URL set through <c>urls</c>.
    /// </summary>
    public static WebApplicationBuilder AddLocalNetworkListening(this WebApplicationBuilder builder)
    {
        builder.Services.AddOptions<NetworkOptions>()
            .BindConfiguration(NetworkOptions.SectionName)
            .Validate(options => options.Port is >= 1 and <= 65535, $"{NetworkOptions.SectionName}:Port must be between 1 and 65535")
            .ValidateOnStart();

        builder.Services.AddOptions<KestrelServerOptions>()
            .Configure<IOptions<NetworkOptions>>((kestrel, network) => kestrel.Listen(IPAddress.Any, network.Value.Port));

        return builder;
    }
}
