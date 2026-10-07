using PartyGame.Contracts;
using PartyGame.Engine.State;

namespace PartyGame.Server.Incidents;

/// <summary>
/// Tells the game master that the server recovered from a failure during the game. The caller logs the failure itself:
/// an incident carries no trace of the exception.
/// </summary>
internal interface IIncidentReporter
{
    /// <summary>
    /// Records an incident and sends every incident to the consoles of the game master. Never throws because a console
    /// cannot be reached: the list waits for its next announcement.
    /// </summary>
    /// <param name="code">What went wrong.</param>
    /// <param name="state">The state of the game when it happened, which tells the round in progress.</param>
    /// <param name="role">The role whose snapshot could not be projected, if that is what went wrong.</param>
    /// <param name="cancellationToken">Cancelled when the server stops.</param>
    ValueTask ReportAsync(IncidentCode code, GameState state, Role? role, CancellationToken cancellationToken);

    /// <summary>
    /// Records an incident met outside the handling of an input, whose round the caller tells: one a client met, such as
    /// the TV screen that could not show a round, or the persistence of the game that failed. Sends every incident to the
    /// consoles of the game master, as <see cref="ReportAsync(IncidentCode, GameState, Role?, CancellationToken)"/> does.
    /// </summary>
    /// <param name="code">What went wrong.</param>
    /// <param name="round">The round in progress when it happened, or <see langword="null"/> outside a round.</param>
    /// <param name="step">The step of the round concerned, if the incident names one.</param>
    /// <param name="cancellationToken">Cancelled when the server stops.</param>
    ValueTask ReportIncidentAsync(IncidentCode code, RoundInfo? round, int? step, CancellationToken cancellationToken);

    /// <summary>
    /// Forgets the incidents of a code once what went wrong is over, such as the persistence of the game that works again,
    /// and sends every incident left to the consoles of the game master. Does nothing when no incident has this code.
    /// </summary>
    /// <param name="code">What no longer goes wrong.</param>
    /// <param name="cancellationToken">Cancelled when the server stops.</param>
    ValueTask ResolveAsync(IncidentCode code, CancellationToken cancellationToken);
}
