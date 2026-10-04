using System.Security.Cryptography;

namespace PartyGame.Server.Network;

internal static class NetworkDiagnosticExtensions
{
    /// <summary>What the diagnostic page downloads to measure the throughput of the network.</summary>
    public static readonly string DownloadPath = $"{ServerPaths.Api}/diagnostic/download";

    /// <summary>About 1 MB: long enough to measure, short enough not to slow down the Wi-Fi of the venue.</summary>
    public const int DownloadBytes = 1 << 20;

    /// <summary>
    /// Keeps how the devices reach the server, and sends it to the game master every few seconds.
    /// </summary>
    public static WebApplicationBuilder AddNetworkDiagnostic(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<NetworkHealthJournal>();
        builder.Services.AddHostedService<NetworkHealthBroadcaster>();
        return builder;
    }

    /// <summary>Serves the download of the diagnostic page, never from a cache.</summary>
    public static WebApplication MapNetworkDiagnostic(this WebApplication app)
    {
        // Random bytes: nothing on the way can compress them into a faster network than it is.
        var payload = RandomNumberGenerator.GetBytes(DownloadBytes);
        app.MapGet(DownloadPath, (HttpContext context) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            return Results.Bytes(payload, "application/octet-stream");
        });
        return app;
    }
}
