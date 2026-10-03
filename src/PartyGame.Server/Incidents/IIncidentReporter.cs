using PartyGame.Contracts;
using PartyGame.Engine;

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
}
