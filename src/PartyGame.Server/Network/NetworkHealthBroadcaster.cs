using Microsoft.AspNetCore.SignalR;
using PartyGame.Contracts;
using PartyGame.Server.Hubs;

namespace PartyGame.Server.Network;

/// <summary>
/// Sends the <see cref="NetworkHealthJournal"/> to the group of the game master every few seconds, never to the TV screen
/// nor the phones. Sent whole and unconditionally: a list that arrived out of order is replaced at the next tick, and no
/// flood of reports turns into a flood of messages.
/// </summary>
internal sealed class NetworkHealthBroadcaster(
    NetworkHealthJournal journal,
    IHubContext<GameHub, IGameClient> hub,
    TimeProvider timeProvider,
    ILogger<NetworkHealthBroadcaster> logger) : BackgroundService
{
    public static readonly TimeSpan Period = TimeSpan.FromSeconds(2);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Period, timeProvider);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
            {
                try
                {
                    await hub.Clients.Group(HubGroups.GameMaster).ReceiveNetworkHealth(journal.Current).ConfigureAwait(false);
                }
                catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                {
                    // The next tick sends it again.
                    logger.NetworkHealthNotSent(ex);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // The server is stopping.
        }
    }
}
