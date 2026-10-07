using PartyGame.Contracts;
using PartyGame.Engine.State;
using PartyGame.Server.Incidents;

namespace PartyGame.Server.Tests.Games;

/// <summary>
/// Records the incidents reported, and fails to report them when asked to.
/// </summary>
internal sealed class RecordingIncidentReporter(bool fails = false) : IIncidentReporter
{
    public List<(IncidentCode Code, GameState State, Role? Role)> Reported { get; } = [];

    public ValueTask ReportAsync(IncidentCode code, GameState state, Role? role, CancellationToken cancellationToken)
    {
        if (fails)
        {
            throw new InvalidOperationException("Injected reporter failure");
        }

        Reported.Add((code, state, role));
        return ValueTask.CompletedTask;
    }

    public List<IncidentCode> Detached { get; } = [];

    public List<IncidentCode> Resolved { get; } = [];

    public ValueTask ReportIncidentAsync(IncidentCode code, RoundInfo? round, int? step, CancellationToken cancellationToken)
    {
        Detached.Add(code);
        return ValueTask.CompletedTask;
    }

    public ValueTask ResolveAsync(IncidentCode code, CancellationToken cancellationToken)
    {
        Resolved.Add(code);
        return ValueTask.CompletedTask;
    }
}
