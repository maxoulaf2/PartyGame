using System.Net;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Options;

namespace PartyGame.Server.Network;

internal static class NetworkExtensions
{
    public const string AdvertisedAddressSetting = $"{NetworkOptions.SectionName}:{nameof(NetworkOptions.AdvertisedAddress)}";

    /// <summary>
    /// Listens over HTTP on every IPv4 interface, so that phones on the local network reach the server,
    /// on the port given by <see cref="NetworkOptions"/>. This replaces any URL set through <c>urls</c>.
    /// Also selects, once, the address advertised to phones.
    /// </summary>
    public static WebApplicationBuilder AddLocalNetworkListening(this WebApplicationBuilder builder)
    {
        builder.Services.AddOptions<NetworkOptions>()
            .BindConfiguration(NetworkOptions.SectionName)
            .Validate(options => options.Port is >= 1 and <= 65535, $"{NetworkOptions.SectionName}:Port must be between 1 and 65535")
            .Validate(options => options.TryGetAdvertisedAddress(out _), $"{AdvertisedAddressSetting} must be an IPv4 address such as 192.168.1.42")
            .ValidateOnStart();

        builder.Services.AddOptions<KestrelServerOptions>()
            .Configure<IOptions<NetworkOptions>>((kestrel, network) => kestrel.Listen(IPAddress.Any, network.Value.Port));

        builder.Services.AddSingleton<INetworkInterfaceSource, SystemNetworkInterfaceSource>();
        builder.Services.AddSingleton(SelectAddress);

        return builder;
    }

    private static AddressSelection SelectAddress(IServiceProvider services)
    {
        var options = services.GetRequiredService<IOptions<NetworkOptions>>().Value;
        var logger = services.GetRequiredService<ILogger<AddressSelection>>();
        options.TryGetAdvertisedAddress(out var configured);

        var selection = AddressSelector.Select(services.GetRequiredService<INetworkInterfaceSource>().GetInterfaces(), configured);

        if (selection.Address is null)
        {
            logger.NoPrivateAddress();
        }
        else
        {
            logger.AddressAdvertised(selection.Address, selection.IsConfigured);
            if (!selection.IsOnActiveInterface)
            {
                logger.ConfiguredAddressNotOnInterface(selection.Address, AdvertisedAddressSetting);
            }
        }

        return selection;
    }
}
