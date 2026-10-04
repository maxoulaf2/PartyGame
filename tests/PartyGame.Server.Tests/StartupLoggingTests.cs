using Microsoft.AspNetCore.Mvc.Testing;

namespace PartyGame.Server.Tests;

public sealed class StartupLoggingTests : IDisposable
{
    private readonly TempDirectory _logs = new();

    public void Dispose() => _logs.Dispose();

    [Fact]
    public async Task Startup_ServerStarted_WritesLifecycleMessageToLogFile()
    {
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseScratchDirectory(_logs.Path));

        using var client = factory.CreateClient();
        using var response = await client.GetAsync(new Uri("/health", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Contains("PartyGame server starting", _logs.ReadAllLogs(), StringComparison.Ordinal);
    }
}
