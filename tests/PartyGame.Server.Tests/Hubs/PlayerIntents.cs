using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;
using PartyGame.Contracts;
using PartyGame.Contracts.Serialization;
using PartyGame.Server.Hubs;

namespace PartyGame.Server.Tests.Hubs;

/// <summary>
/// Sends the round intents of players as their phones do: each one in its envelope, numbered one more than the previous
/// one sent through the same connection.
/// </summary>
internal sealed class PlayerIntents
{
    private readonly Dictionary<HubConnection, long> _lastClientSeqs = [];

    /// <summary>Sends the next intent of the player, and returns once the hub acknowledged it.</summary>
    public Task SendAsync(HubConnection player, PlayerRoundIntent intent)
    {
        var clientSeq = _lastClientSeqs.GetValueOrDefault(player) + 1;
        _lastClientSeqs[player] = clientSeq;
        return SendAsync(player, clientSeq, intent);
    }

    /// <summary>Sends an intent with the given number, as a phone sends again one it got no acknowledgment for.</summary>
    public static Task SendAsync(HubConnection player, long clientSeq, PlayerRoundIntent intent) =>
        player.InvokeAsync(
            GameHub.SendRoundIntent,
            JsonSerializer.SerializeToElement(new PlayerIntentEnvelope(clientSeq, intent), ContractJsonOptions.Default),
            TestContext.Current.CancellationToken);
}
