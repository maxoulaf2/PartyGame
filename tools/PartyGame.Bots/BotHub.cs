using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using PartyGame.Contracts.Serialization;

namespace PartyGame.Bots;

/// <summary>
/// Connections of the bots to the hub, set up as the browsers set theirs up.
/// </summary>
internal static class BotHub
{
    /// <summary>
    /// Long enough to outlast a restart of the server, which the phones wait for as well.
    /// </summary>
    private static readonly TimeSpan[] _reconnectDelays = [TimeSpan.Zero, .. Enumerable.Repeat(TimeSpan.FromSeconds(2), 60)];

    /// <param name="serverUrl">The address of the server, as the QR code gives it.</param>
    /// <param name="configure">Changes the transport, for a test server reached in memory.</param>
    public static HubConnection Create(Uri serverUrl, Action<HttpConnectionOptions>? configure) =>
        new HubConnectionBuilder()
            .WithUrl(new Uri(serverUrl, "/hub/game"), options => configure?.Invoke(options))
            .WithAutomaticReconnect(_reconnectDelays)
            .AddJsonProtocol(options => ContractJsonOptions.Apply(options.PayloadSerializerOptions))
            .Build();

    /// <summary>
    /// Starts the connection, with a message for the operator when the server cannot be reached.
    /// </summary>
    public static async Task StartAsync(HubConnection connection, Uri serverUrl, CancellationToken cancellationToken)
    {
        try
        {
            await connection.StartAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            throw new BotException($"Serveur injoignable à l'adresse {serverUrl} : {ex.Message}");
        }
    }
}
