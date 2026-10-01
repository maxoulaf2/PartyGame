using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using PartyGame.Contracts.Serialization;

namespace PartyGame.Server.Tests.Hubs;

/// <summary>
/// Real SignalR clients connected to the in-memory test server, as the browsers connect to the real one.
/// </summary>
internal static class HubClients
{
    /// <summary>
    /// Starts a connection restricted to WebSockets: it only succeeds if the hub accepts that transport.
    /// </summary>
    public static async Task<HubConnection> ConnectAsync(WebApplicationFactory<Program> factory, string path = "/hub/game")
    {
        var server = factory.Server;
        var connection = new HubConnectionBuilder()
            .WithUrl(new Uri(server.BaseAddress, path), options =>
            {
                options.Transports = HttpTransportType.WebSockets;
                options.HttpMessageHandlerFactory = _ => server.CreateHandler();
                options.WebSocketFactory = async (context, cancellationToken) =>
                    await server.CreateWebSocketClient().ConnectAsync(context.Uri, cancellationToken).ConfigureAwait(false);
            })
            .AddJsonProtocol(options => ContractJsonOptions.Apply(options.PayloadSerializerOptions))
            .Build();

        await connection.StartAsync(TestContext.Current.CancellationToken);
        return connection;
    }

    /// <summary>
    /// Sends a probe to each of <paramref name="groups"/>, then returns those the connection received it from.
    /// A last message sent to the connection alone marks the end: messages to one connection arrive in order.
    /// </summary>
    public static async Task<IReadOnlyList<string>> GroupsOfAsync<THub>(
        WebApplicationFactory<Program> factory,
        HubConnection connection,
        params string[] groups)
        where THub : Microsoft.AspNetCore.SignalR.Hub
    {
        const string Probe = "Probe";
        const string End = "<end>";

        var received = new List<string>();
        var ended = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var subscription = connection.On<string>(Probe, group =>
        {
            if (group == End)
            {
                ended.TrySetResult();
            }
            else
            {
                received.Add(group);
            }
        });

        var clients = factory.Services.GetRequiredService<Microsoft.AspNetCore.SignalR.IHubContext<THub>>().Clients;
        var cancellationToken = TestContext.Current.CancellationToken;
        foreach (var group in groups)
        {
            await clients.Group(group).SendCoreAsync(Probe, [group], cancellationToken);
        }

        await clients.Client(connection.ConnectionId!).SendCoreAsync(Probe, [End], cancellationToken);
        await ended.Task.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
        return received;
    }
}
