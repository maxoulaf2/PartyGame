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

    // The token is never logged: it is the only proof of who a player is.
    [LoggerMessage(Level = LogLevel.Information, Message = "Player {PlayerId} joined as {Nickname}")]
    public static partial void PlayerJoined(this ILogger logger, Guid playerId, string nickname);

    [LoggerMessage(Level = LogLevel.Information, Message = "Player {PlayerId} lost their last connection")]
    public static partial void PlayerDisconnected(this ILogger logger, Guid playerId);

    // The token is never logged, known or not.
    [LoggerMessage(Level = LogLevel.Information, Message = "Player {PlayerId} resumed their session on connection {ConnectionId}")]
    public static partial void SessionResumed(this ILogger logger, Guid playerId, string connectionId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "ResumeSession from connection {ConnectionId} refused: unknown token")]
    public static partial void SessionUnknown(this ILogger logger, string connectionId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "ResumeSession from connection {ConnectionId} ignored: it already identified a player")]
    public static partial void ResumeRepeated(this ILogger logger, string connectionId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "JoinGame from connection {ConnectionId} ignored: it already registered a player")]
    public static partial void JoinRepeated(this ILogger logger, string connectionId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Player {PlayerId} renamed {Nickname} by the game master")]
    public static partial void PlayerRenamed(this ILogger logger, Guid playerId, string nickname);

    [LoggerMessage(Level = LogLevel.Information, Message = "Game started by the game master with {PlayerCount} players and pack {PackId} ({PackTitle})")]
    public static partial void GameStarted(this ILogger logger, int playerCount, string packId, string packTitle);

    [LoggerMessage(Level = LogLevel.Information, Message = "Pack {PackId} chosen by the game master")]
    public static partial void PackSelected(this ILogger logger, string packId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Packs reloaded by the game master: {PackCount} packs, {ValidPackCount} valid")]
    public static partial void PacksReloaded(this ILogger logger, int packCount, int validPackCount);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Connection {ConnectionId} still runs client build {ClientBuildId} after reloading to get build {ServerBuildId}: a cache or a proxy keeps serving the old pages")]
    public static partial void StaleBuildReported(this ILogger logger, string connectionId, string clientBuildId, string? serverBuildId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Address {Address} advertised to phones, chosen by the game master")]
    public static partial void AdvertisedAddressChosen(this ILogger logger, string address);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Round intent from connection {ConnectionId} ignored: it identified no player")]
    public static partial void RoundIntentWithoutPlayer(this ILogger logger, string connectionId);
}
