using PartyGame.Contracts;

namespace PartyGame.Server.Hubs;

internal static partial class HubLog
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Connection {ConnectionId} announced as {Role}")]
    public static partial void ConnectionAnnounced(this ILogger logger, string connectionId, Role role);

    // The code typed is never logged, wrong or right.
    [LoggerMessage(Level = LogLevel.Warning, Message = "Connection {ConnectionId} gave a wrong game master code")]
    public static partial void GameMasterCodeRejected(this ILogger logger, string connectionId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Malformed {HubMethod} message from connection {ConnectionId} ignored, invalid at {JsonPath}")]
    public static partial void MessageMalformed(this ILogger logger, string hubMethod, string connectionId, string jsonPath);

    [LoggerMessage(Level = LogLevel.Warning, Message = "{HubMethod} from connection {ConnectionId} ignored: not authenticated as game master")]
    public static partial void GameMasterIntentRefused(this ILogger logger, string hubMethod, string connectionId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Hub method {HubMethod} failed for connection {ConnectionId}")]
    public static partial void HubMethodFailed(this ILogger logger, Exception exception, string hubMethod, string connectionId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Snapshot not sent to group {Group}")]
    public static partial void SnapshotNotSent(this ILogger logger, Exception exception, string group);
}
