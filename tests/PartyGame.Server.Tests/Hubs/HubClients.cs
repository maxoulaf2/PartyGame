using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using PartyGame.Contracts;
using PartyGame.Contracts.Serialization;
using PartyGame.Server.Games;
using PartyGame.Server.Hubs;

namespace PartyGame.Server.Tests.Hubs;

/// <summary>
/// Real SignalR clients connected to the in-memory test server, as the browsers connect to the real one.
/// </summary>
internal static class HubClients
{
    /// <summary>
    /// Starts a connection restricted to WebSockets: it only succeeds if the hub accepts that transport.
    /// </summary>
    /// <param name="factory">The test server.</param>
    /// <param name="path">Where the hub listens.</param>
    /// <param name="beforeStart">
    /// Registers handlers before the connection starts, for the messages the hub sends as soon as it is established.
    /// </param>
    /// <param name="userAgent">The user agent of the browser the connection stands for, if any.</param>
    public static async Task<HubConnection> ConnectAsync(
        WebApplicationFactory<Program> factory,
        string path = "/hub/game",
        Action<HubConnection>? beforeStart = null,
        string? userAgent = null)
    {
        var server = factory.Server;
        var connection = new HubConnectionBuilder()
            .WithUrl(new Uri(server.BaseAddress, path), options =>
            {
                options.Transports = HttpTransportType.WebSockets;
                options.HttpMessageHandlerFactory = _ => server.CreateHandler();
                options.WebSocketFactory = async (context, cancellationToken) =>
                {
                    var client = server.CreateWebSocketClient();
                    if (userAgent is not null)
                    {
                        client.ConfigureRequest = request => request.Headers.UserAgent = userAgent;
                    }

                    return await client.ConnectAsync(context.Uri, cancellationToken).ConfigureAwait(false);
                };
            })
            .AddJsonProtocol(options => ContractJsonOptions.Apply(options.PayloadSerializerOptions))
            .Build();

        beforeStart?.Invoke(connection);
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

    /// <summary>
    /// Waits until the connection has received every message sent to it so far: messages to one connection arrive in
    /// order, so a last message sent to it alone marks the end.
    /// </summary>
    public static async Task FlushAsync<THub>(WebApplicationFactory<Program> factory, HubConnection connection)
        where THub : Microsoft.AspNetCore.SignalR.Hub
    {
        const string Flush = "Flush";

        var flushed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var subscription = connection.On(Flush, flushed.SetResult);

        var clients = factory.Services.GetRequiredService<Microsoft.AspNetCore.SignalR.IHubContext<THub>>().Clients;
        var cancellationToken = TestContext.Current.CancellationToken;
        await clients.Client(connection.ConnectionId!).SendCoreAsync(Flush, [], cancellationToken);
        await flushed.Task.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
    }

    /// <summary>
    /// Starts the game from the console of the game master, then the round it announces: what a test needs when it plays
    /// the first round rather than its introduction.
    /// </summary>
    /// <param name="gameMaster">A connection announced as game master.</param>
    /// <param name="game">The loop of the test server, which tells the round announced.</param>
    /// <param name="cancellationToken">Cancels the calls.</param>
    public static async Task<StartGameResult> StartGameAndFirstRoundAsync(this HubConnection gameMaster, GameLoop game, CancellationToken cancellationToken)
    {
        var result = await gameMaster.InvokeAsync<StartGameResult>(GameHub.StartGame, cancellationToken);
        if (result.Refusal is null)
        {
            await gameMaster.InvokeAsync(GameHub.StartRound, new StartRoundRequest(game.State.CurrentRound!.Id), cancellationToken);
        }

        return result;
    }
}
