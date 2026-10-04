using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;
using PartyGame.Contracts;
using PartyGame.Contracts.Serialization;

namespace PartyGame.Server.Tests.Hubs;

/// <summary>
/// Records the snapshots a test connection receives, as raw JSON, so that tests can check both their content and what
/// went over the wire. The lists of incidents are recorded as well: they are the other state the server sends to a role.
/// So is the network health, apart: sent every few seconds, it would make any count of messages depend on time.
/// </summary>
internal sealed class ReceivedSnapshots : IDisposable
{
    private readonly Lock _gate = new();
    private readonly List<(string Message, string Json)> _received = [];
    private readonly IDisposable[] _subscriptions;

    public ReceivedSnapshots(HubConnection connection)
    {
        _subscriptions =
        [
            Subscribe(connection, nameof(IGameClient.ReceiveDisplaySnapshot)),
            Subscribe(connection, nameof(IGameClient.ReceiveGameMasterSnapshot)),
            Subscribe(connection, nameof(IGameClient.ReceivePlayerSnapshot)),
            Subscribe(connection, nameof(IGameClient.ReceiveIncidents)),
            Subscribe(connection, nameof(IGameClient.ReceiveNetworkHealth)),
        ];
    }

    /// <summary>Every snapshot received so far, as JSON, whatever its role.</summary>
    public IReadOnlyList<string> Json
    {
        get
        {
            lock (_gate)
            {
                return [.. _received.Where(r => r.Message.EndsWith("Snapshot", StringComparison.Ordinal)).Select(r => r.Json)];
            }
        }
    }

    /// <summary>Every snapshot and list of incidents received so far, as JSON.</summary>
    public IReadOnlyList<string> Messages
    {
        get
        {
            lock (_gate)
            {
                return [.. _received.Where(r => r.Message != nameof(IGameClient.ReceiveNetworkHealth)).Select(r => r.Json)];
            }
        }
    }

    public IReadOnlyList<DisplaySnapshot> Display => Of<DisplaySnapshot>(nameof(IGameClient.ReceiveDisplaySnapshot));

    public IReadOnlyList<GameMasterSnapshot> GameMaster => Of<GameMasterSnapshot>(nameof(IGameClient.ReceiveGameMasterSnapshot));

    public IReadOnlyList<PlayerSnapshot> Player => Of<PlayerSnapshot>(nameof(IGameClient.ReceivePlayerSnapshot));

    public IReadOnlyList<IncidentList> Incidents => Of<IncidentList>(nameof(IGameClient.ReceiveIncidents));

    public IReadOnlyList<NetworkHealth> NetworkHealth => Of<NetworkHealth>(nameof(IGameClient.ReceiveNetworkHealth));

    public void Dispose()
    {
        foreach (var subscription in _subscriptions)
        {
            subscription.Dispose();
        }
    }

    private IDisposable Subscribe(HubConnection connection, string message) =>
        connection.On<JsonElement>(message, json =>
        {
            lock (_gate)
            {
                _received.Add((message, json.GetRawText()));
            }
        });

    private List<T> Of<T>(string message)
    {
        lock (_gate)
        {
            return [.. _received
                .Where(r => r.Message == message)
                .Select(r => JsonSerializer.Deserialize<T>(r.Json, ContractJsonOptions.Default)!)];
        }
    }
}
