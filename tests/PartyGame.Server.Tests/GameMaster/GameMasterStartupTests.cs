using System.Globalization;
using System.Text.RegularExpressions;

namespace PartyGame.Server.Tests.GameMaster;

public sealed partial class GameMasterStartupTests : IDisposable
{
    private static readonly TimeSpan _startupTimeout = TimeSpan.FromSeconds(30);

    private readonly TempDirectory _logs = new();

    public void Dispose() => _logs.Dispose();

    [Fact]
    public async Task Startup_NoCodeConfigured_PrintsGeneratedCodeOnConsoleButNotInLogFiles()
    {
        var port = ServerProcess.GetFreePort();
        using var server = ServerProcess.Start(ServerEnvironment(port));
        using var client = new HttpClient();

        using var response = await server.WaitForResponseAsync(client, new Uri($"http://localhost:{port}/health"), _startupTimeout);
        var output = await server.WaitForOutputAsync("Code game master", _startupTimeout);

        var banner = BannerCode().Match(output);
        Assert.True(banner.Success, output);
        Assert.Equal(string.Empty, banner.Groups["origin"].Value.Trim());
        AssertNotInLogFiles(banner.Groups["code"].Value);
    }

    [Fact]
    public async Task Startup_CodeConfigured_PrintsItWithItsOriginButNotInLogFiles()
    {
        var port = ServerProcess.GetFreePort();
        var environment = ServerEnvironment(port);
        environment["GameMaster__Code"] = "482913";
        using var server = ServerProcess.Start(environment);
        using var client = new HttpClient();

        using var response = await server.WaitForResponseAsync(client, new Uri($"http://localhost:{port}/health"), _startupTimeout);
        var output = await server.WaitForOutputAsync("Code game master", _startupTimeout);

        Assert.Contains("482913 (imposé par GameMaster:Code)", output, StringComparison.Ordinal);
        AssertNotInLogFiles("482913");
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("abcdef")]
    public async Task Startup_InvalidConfiguredCode_ExitsNamingTheSetting(string configured)
    {
        var environment = ServerEnvironment(ServerProcess.GetFreePort());
        environment["GameMaster__Code"] = configured;
        using var server = ServerProcess.Start(environment);

        var exitCode = await server.WaitForExitAsync(_startupTimeout);

        Assert.Equal(1, exitCode);
        Assert.Contains("GameMaster:Code must be made of exactly 6 digits", server.Output, StringComparison.Ordinal);
    }

    // Log timestamps are digits too: only a standalone run of six digits would be the code.
    private void AssertNotInLogFiles(string code)
    {
        var logs = _logs.ReadAllLogs();
        Assert.NotEmpty(logs);
        Assert.DoesNotMatch($"(?<![0-9]){code}(?![0-9])", logs);
    }

    private Dictionary<string, string> ServerEnvironment(int port) => new()
    {
        ["Network__Port"] = port.ToString(CultureInfo.InvariantCulture),
        ["LogFiles__Directory"] = _logs.Path,
    };

    [GeneratedRegex(@"Code game master\s*: (?<code>[0-9]{6})(?<origin>[^\r\n]*)")]
    private static partial Regex BannerCode();
}
