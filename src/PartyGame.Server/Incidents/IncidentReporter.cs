using Microsoft.AspNetCore.SignalR;
using PartyGame.Contracts;
using PartyGame.Engine.Projections;
using PartyGame.Engine.State;
using PartyGame.Server.Hubs;

namespace PartyGame.Server.Incidents;

/// <summary>
/// Records the incidents in the <see cref="IncidentJournal"/> and sends the whole list to the group of the game master,
/// never to the TV screen nor the phones.
/// </summary>
internal sealed class IncidentReporter(
    IncidentJournal journal,
    IHubContext<GameHub, IGameClient> hub,
    ILogger<IncidentReporter> logger)
    : IIncidentReporter
{
    public ValueTask ReportAsync(IncidentCode code, GameState state, Role? role, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(state);

        // Between two rounds, the last round played is over: what fails then is not part of it. A round announced is, since
        // what fails then is its start, and the game master may skip it.
        var round = state.Phase is GamePhase.Round or GamePhase.RoundIntro ? Snapshots.RoundInfoOf(state) : null;
        return SendAsync(code, journal.Record(code, round, role), cancellationToken);
    }

    public ValueTask ReportIncidentAsync(IncidentCode code, RoundInfo? round, int? step, CancellationToken cancellationToken) =>
        SendAsync(code, journal.Record(code, round, role: null, step), cancellationToken);

    public ValueTask ResolveAsync(IncidentCode code, CancellationToken cancellationToken) =>
        journal.Resolve(code) is { } incidents ? SendAsync(code, incidents, cancellationToken) : ValueTask.CompletedTask;

    private async ValueTask SendAsync(IncidentCode code, IncidentList incidents, CancellationToken cancellationToken)
    {
        try
        {
            await hub.Clients.Group(HubGroups.GameMaster).ReceiveIncidents(incidents).ConfigureAwait(false);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            // The consoles get the list when they announce themselves again.
            logger.IncidentsNotSent(ex, code);
        }
    }
}
