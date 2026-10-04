namespace PartyGame.Server.Logging;

internal static partial class LifecycleLog
{
    [LoggerMessage(Level = LogLevel.Information, Message = "PartyGame server starting in {Environment} environment")]
    public static partial void ServerStarting(this ILogger logger, string environment);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Fault injection active: input {InputType} throws {FailCount} time(s)")]
    public static partial void FaultInjectionActive(this ILogger logger, string inputType, int failCount);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Fault injection requested by {Setting} but ignored in Production environment")]
    public static partial void FaultInjectionIgnored(this ILogger logger, string setting);
}
