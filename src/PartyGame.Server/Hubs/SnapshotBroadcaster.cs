using Microsoft.AspNetCore.SignalR;
using PartyGame.Contracts;
using PartyGame.Engine;
using PartyGame.Engine.Projections;
using PartyGame.Server.Games;

namespace PartyGame.Server.Hubs;

/// <summary>
/// Sends the new state to every client after each change, each group getting the projection of its role only: the TV
/// screen, the game master, and each player in a group of their own.
/// </summary>
/// <remarks>
/// A failed send is logged and skipped: the client keeps its last snapshot and gets the current one when it comes back.
/// </remarks>
internal sealed class SnapshotBroadcaster(
    IHubContext<GameHub, IGameClient> hub,
    Snapshots snapshots,
    ILogger<SnapshotBroadcaster> logger)
    : IGameStateListener
{
    public async ValueTask OnStateChangedAsync(GameState state, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(state);

        await SendAsync(HubGroups.Display, client => client.ReceiveDisplaySnapshot(snapshots.ForDisplay(state)), cancellationToken)
            .ConfigureAwait(false);
        await SendAsync(HubGroups.GameMaster, client => client.ReceiveGameMasterSnapshot(snapshots.ForGameMaster(state)), cancellationToken)
            .ConfigureAwait(false);
        foreach (var player in state.Players)
        {
            await SendAsync(HubGroups.Player(player.Id), client => client.ReceivePlayerSnapshot(snapshots.ForPlayer(state, player)), cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private async Task SendAsync(string group, Func<IGameClient, Task> send, CancellationToken cancellationToken)
    {
        try
        {
            await send(hub.Clients.Group(group)).ConfigureAwait(false);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            // One unreachable group must not deprive the others of the new state.
            logger.SnapshotNotSent(ex, group);
        }
    }
}
