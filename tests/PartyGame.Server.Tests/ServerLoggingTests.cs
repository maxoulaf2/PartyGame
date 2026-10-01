using System.Text.Json;
using Microsoft.Extensions.Configuration;
using PartyGame.Server.Logging;

namespace PartyGame.Server.Tests;

public sealed class ServerLoggingTests : IDisposable
{
    private readonly TempDirectory _logs = new();

    public void Dispose() => _logs.Dispose();

    [Fact]
    public void CreateLogger_TemplatedMessage_KeepsPropertyStructuredInFile()
    {
        using (var logger = ServerLogging.CreateLogger(Configuration()))
        {
            logger.Information("Player {PlayerId} joined", "p-42");
        }

        var line = Assert.Single(_logs.ReadAllLogs().Split('\n', StringSplitOptions.RemoveEmptyEntries));
        using var json = JsonDocument.Parse(line);
        Assert.Equal("Player {PlayerId} joined", json.RootElement.GetProperty("@mt").GetString());
        Assert.Equal("p-42", json.RootElement.GetProperty("PlayerId").GetString());
    }

    [Fact]
    public void CreateLogger_FileSizeLimitReached_RollsAndDeletesOldestFiles()
    {
        var configuration = Configuration(new()
        {
            ["LogFiles:FileSizeLimitBytes"] = "1024",
            ["LogFiles:RetainedFileCount"] = "3",
        });

        using (var logger = ServerLogging.CreateLogger(configuration))
        {
            for (var i = 0; i < 200; i++)
            {
                logger.Information("Filler message {Index} to force rolling", i);
            }
        }

        Assert.Equal(3, _logs.LogFiles().Length);
    }

    [Theory]
    [InlineData("Information", false)]
    [InlineData("Debug", true)]
    public void CreateLogger_MinimumLevelFromConfiguration_FiltersDebugMessages(string minimumLevel, bool expectDebug)
    {
        var configuration = Configuration(new() { ["Serilog:MinimumLevel:Default"] = minimumLevel });

        using (var logger = ServerLogging.CreateLogger(configuration))
        {
            logger.Debug("Debug marker");
            logger.Information("Information marker");
        }

        var content = _logs.ReadAllLogs();
        Assert.Contains("Information marker", content, StringComparison.Ordinal);
        Assert.Equal(expectDebug, content.Contains("Debug marker", StringComparison.Ordinal));
    }

    [Fact]
    public void CreateLogger_LogDirectoryNotWritable_KeepsLoggingWithoutThrowing()
    {
        // A file where the log directory should be makes directory creation fail on every OS.
        Directory.CreateDirectory(_logs.Path);
        var blocked = Path.Combine(_logs.Path, "blocked");
        File.WriteAllText(blocked, string.Empty);

        var configuration = Configuration(new() { ["LogFiles:Directory"] = Path.Combine(blocked, "logs") });

        var exception = Record.Exception(() =>
        {
            using var logger = ServerLogging.CreateLogger(configuration);
            logger.Information("Message that cannot reach the file");
        });

        Assert.Null(exception);
    }

    private IConfiguration Configuration(Dictionary<string, string?>? overrides = null)
    {
        var values = new Dictionary<string, string?>
        {
            ["Serilog:MinimumLevel:Default"] = "Debug",
            ["LogFiles:Directory"] = _logs.Path,
        };
        foreach (var (key, value) in overrides ?? [])
        {
            values[key] = value;
        }

        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }
}
