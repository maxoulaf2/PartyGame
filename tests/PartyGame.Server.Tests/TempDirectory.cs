namespace PartyGame.Server.Tests;

internal sealed class TempDirectory : IDisposable
{
    public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "partygame-tests", Guid.NewGuid().ToString("N"));

    public string[] LogFiles() =>
        Directory.Exists(Path) ? Directory.GetFiles(Path, "partygame-*.log") : [];

    public string ReadAllLogs() =>
        string.Concat(LogFiles().Order(StringComparer.Ordinal).Select(ReadShared));

    public void Dispose()
    {
        // Under WebApplicationFactory the entry point thread disposes the host, and thus closes
        // the log file, slightly after the factory itself is disposed.
        for (var attempt = 1; Directory.Exists(Path); attempt++)
        {
            try
            {
                Directory.Delete(Path, recursive: true);
            }
            catch (IOException) when (attempt < 50)
            {
                Thread.Sleep(100);
            }
        }
    }

    // The server may still hold the current log file open.
    private static string ReadShared(string file)
    {
        using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
