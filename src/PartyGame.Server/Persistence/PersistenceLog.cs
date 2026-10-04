namespace PartyGame.Server.Persistence;

internal static partial class PersistenceLog
{
    [LoggerMessage(Level = LogLevel.Error, Message = "Game could not be saved to {File}: a crash of the server would lose it")]
    public static partial void GameNotSaved(this ILogger logger, Exception exception, string file);

    [LoggerMessage(Level = LogLevel.Information, Message = "Game saved again to {File}")]
    public static partial void GameSavedAgain(this ILogger logger, string file);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Temporary file {File} of an interrupted save deleted")]
    public static partial void TemporaryFileDeleted(this ILogger logger, string file);

    [LoggerMessage(Level = LogLevel.Information, Message = "Saved game found in {File} with {PlayerCount} players, saved at {SavedAt}: waiting for the game master to resume it or start a new game")]
    public static partial void SavedGameFound(this ILogger logger, string file, int playerCount, DateTimeOffset savedAt);

    [LoggerMessage(Level = LogLevel.Information, Message = "Saved game in {File} has no player: a new game starts")]
    public static partial void SavedGameWithoutPlayer(this ILogger logger, string file);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Saved game in {File} cannot be resumed ({Problem}): set aside as {SetAsideFile}, a new game starts")]
    public static partial void SavedGameUnreadable(this ILogger logger, string file, string problem, string setAsideFile);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Saved game in {File} cannot be resumed ({Problem}), nor set aside: a new game starts, and replaces it at its first save")]
    public static partial void SavedGameNotSetAside(this ILogger logger, Exception exception, string file, string problem);

    [LoggerMessage(Level = LogLevel.Information, Message = "Saved game set aside as {File}: the game master started a new game")]
    public static partial void SavedGameArchived(this ILogger logger, string file);
}
