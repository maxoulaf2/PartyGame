using PartyGame.Contracts;

namespace PartyGame.Server.Incidents;

internal static partial class IncidentLog
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Incidents not sent to the game master after incident {IncidentCode}")]
    public static partial void IncidentsNotSent(this ILogger logger, Exception exception, IncidentCode incidentCode);

    [LoggerMessage(Level = LogLevel.Error, Message = "Incident {IncidentCode} could not be reported")]
    public static partial void IncidentNotReported(this ILogger logger, Exception exception, IncidentCode incidentCode);
}
