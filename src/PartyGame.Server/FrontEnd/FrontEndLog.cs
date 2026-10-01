namespace PartyGame.Server.FrontEnd;

internal static partial class FrontEndLog
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Client build not found in {WebRoot}: pages will answer 404 until 'npm run build' is run in client/")]
    public static partial void FrontEndBuildMissing(this ILogger logger, string webRoot);
}
