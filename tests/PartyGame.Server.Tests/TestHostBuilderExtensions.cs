using Microsoft.AspNetCore.Hosting;
using PartyGame.Server.Persistence;

namespace PartyGame.Server.Tests;

internal static class TestHostBuilderExtensions
{
    /// <summary>
    /// Writes the log files and the saved game of a test server to a folder of its own: test servers run in parallel.
    /// </summary>
    public static IWebHostBuilder UseScratchDirectory(this IWebHostBuilder builder, string path) =>
        builder.UseSetting("LogFiles:Directory", path).UseSetting(PersistenceOptions.DirectorySetting, path);
}
