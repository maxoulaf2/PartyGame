namespace PartyGame.Server.Logging;

internal sealed class LogFileOptions
{
    public const string SectionName = "LogFiles";

    // Relative paths are resolved against the executable folder, not the working directory,
    // so a systemd service on the Raspberry Pi writes next to the binaries.
    public string Directory { get; init; } = "logs";

    public long FileSizeLimitBytes { get; init; } = 10 * 1024 * 1024;

    public int RetainedFileCount { get; init; } = 14;
}
