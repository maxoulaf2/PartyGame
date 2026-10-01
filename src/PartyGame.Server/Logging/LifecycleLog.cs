namespace PartyGame.Server.Logging;

internal static partial class LifecycleLog
{
    [LoggerMessage(Level = LogLevel.Information, Message = "PartyGame server starting in {Environment} environment")]
    public static partial void ServerStarting(this ILogger logger, string environment);
}
