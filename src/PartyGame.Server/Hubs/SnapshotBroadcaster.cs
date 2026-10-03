using Microsoft.AspNetCore.SignalR;
using PartyGame.Contracts;
using PartyGame.Engine;
using PartyGame.Engine.Projections;
using PartyGame.Server.Games;
using PartyGame.Server.Incidents;

namespace PartyGame.Server.Hubs;

/// <summary>
/// Sends the new state to every client after each change, each group getting the projection of its role only: the TV
/// screen, the game master, and each player in a group of their own.
/// </summary>
/// <remarks>
/// A failed send is logged and skipped: the client keeps its last snapshot and gets the current one when it comes back. A
/// projection that throws is a bug: its clients keep their last snapshot too, the others get theirs, and the game master is
/// told which role went without.
/// </remarks>
internal sealed class SnapshotBroadcaster(
    IHubContext<GameHub, IGameClient> hub,
    Snapshots snapshots,
    IIncidentReporter incidents,
    ILogger<SnapshotBroadcaster> logger)
    : IGameStateListener
{
    public async ValueTask OnStateChangedAsync(GameState state, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (await ProjectAsync(state, Role.Display, () => snapshots.ForDisplay(state), cancellationToken).ConfigureAwait(false) is { } display)
        {
            await SendAsync(HubGroups.Display, client => client.ReceiveDisplaySnapshot(display), cancellationToken).ConfigureAwait(false);
        }

        if (await ProjectAsync(state, Role.GameMaster, () => snapshots.ForGameMaster(state), cancellationToken).ConfigureAwait(false) is { } gameMaster)
        {
            await SendAsync(HubGroups.GameMaster, client => client.ReceiveGameMasterSnapshot(gameMaster), cancellationToken).ConfigureAwait(false);
        }

        var playerProjectionFailed = false;
        foreach (var player in state.Players)
        {
            PlayerSnapshot snapshot;
            try
            {
                snapshot = snapshots.ForPlayer(state, player);
            }
            catch (Exception ex)
            {
                logger.ProjectionFailed(ex, Role.Player, state.Version);
                playerProjectionFailed = true;
                continue;
            }

            await SendAsync(HubGroups.Player(player.Id), client => client.ReceivePlayerSnapshot(snapshot), cancellationToken).ConfigureAwait(false);
        }

        if (playerProjectionFailed)
        {
            // Once per change, however many phones went without: the game master can do nothing more about each one.
            await incidents.ReportAsync(IncidentCode.ProjectionFailed, state, Role.Player, cancellationToken).ConfigureAwait(false);
        }
    }

    private async ValueTask<T?> ProjectAsync<T>(GameState state, Role role, Func<T> project, CancellationToken cancellationToken)
        where T : class
    {
        try
        {
            return project();
        }
        catch (Exception ex)
        {
            logger.ProjectionFailed(ex, role, state.Version);
            await incidents.ReportAsync(IncidentCode.ProjectionFailed, state, role, cancellationToken).ConfigureAwait(false);
            return null;
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
