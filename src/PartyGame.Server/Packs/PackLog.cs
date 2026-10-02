using PartyGame.Contracts;

namespace PartyGame.Server.Packs;

internal static partial class PackLog
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Pack {PackId} loaded from {Folder}: title {Title}, {RoundCount} rounds, {ProblemCount} problems")]
    public static partial void PackLoaded(this ILogger logger, string packId, string folder, string? title, int? roundCount, int problemCount);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Pack {PackId} is invalid: {ProblemCode} in {File} at {Path} {Parameters}")]
    public static partial void PackProblemFound(this ILogger logger, string packId, PackProblemCode problemCode, string file, string path, string parameters);

    [LoggerMessage(Level = LogLevel.Error, Message = "Pack {PackId} failed to load from {Folder}")]
    public static partial void PackLoadFailed(this ILogger logger, Exception exception, string packId, string folder);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Pack directory {Directory} does not exist: no pack can be played. Set {Setting} to the folder of the packs")]
    public static partial void PackDirectoryMissing(this ILogger logger, string directory, string setting);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Pack directory {Directory} holds no pack: no pack can be played. A pack is a subfolder with a pack.json file")]
    public static partial void PackDirectoryEmpty(this ILogger logger, string directory);
}
