namespace PartyGame.Server.FrontEnd;

internal static partial class FrontEndLog
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Client build not found in {WebRoot}: pages will answer 404 until 'npm run build' is run in client/")]
    public static partial void FrontEndBuildMissing(this ILogger logger, string webRoot);

    [LoggerMessage(Level = LogLevel.Information, Message = "Serving client build {BuildId}")]
    public static partial void FrontEndBuildServed(this ILogger logger, string buildId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Client build has no {FileName}: pages cached by browsers will not reload after an update until 'npm run build' is run again in client/")]
    public static partial void FrontEndBuildIdMissing(this ILogger logger, string fileName);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Client build identifier unreadable in {FileName}: pages cached by browsers will not reload after an update until 'npm run build' is run again in client/")]
    public static partial void FrontEndBuildIdUnreadable(this ILogger logger, string fileName);
}
