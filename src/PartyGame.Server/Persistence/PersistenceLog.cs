namespace PartyGame.Server.Persistence;

internal static partial class PersistenceLog
{
    [LoggerMessage(Level = LogLevel.Error, Message = "Game could not be saved to {File}: a crash of the server would lose it")]
    public static partial void GameNotSaved(this ILogger logger, Exception exception, string file);

    [LoggerMessage(Level = LogLevel.Information, Message = "Game saved again to {File}")]
    public static partial void GameSavedAgain(this ILogger logger, string file);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Temporary file {File} of an interrupted save deleted")]
    public static partial void TemporaryFileDeleted(this ILogger logger, string file);
}
