using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using PartyGame.Contracts;

namespace PartyGame.Server.Tests.Hubs;

public sealed class WelcomeTests : IAsyncDisposable
{
    private readonly TempDirectory _webRoot = new();
    private readonly TempDirectory _logs = new();
    private WebApplicationFactory<Program>? _factory;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask DisposeAsync()
    {
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }

        _webRoot.Dispose();
        _logs.Dispose();
    }

    [Fact]
    public async Task Connect_BuildIdentifiedInWebRoot_WelcomesWithThatIdentifier()
    {
        // Given
        WriteClientBuild("""{ "buildId": "2026-10-02T14-30-00Z-ab12cd" }""");
        var factory = CreateFactory();

        // When
        var welcome = await ConnectAndReadWelcomeAsync(factory);

        // Then
        Assert.Equal(new Welcome("2026-10-02T14-30-00Z-ab12cd"), welcome);
    }

    [Fact]
    public async Task Connect_OnTheWire_SendsTheIdentifierAsBuildId()
    {
        // Given
        WriteClientBuild("""{ "buildId": "abc" }""");
        var factory = CreateFactory();
        var received = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);

        // When
        await using var connection = await HubClients.ConnectAsync(
            factory,
            beforeStart: c => c.On<JsonElement>(nameof(IGameClient.ReceiveWelcome), received.SetResult));

        // Then
        var welcome = await received.Task.WaitAsync(TimeSpan.FromSeconds(10), Ct);
        Assert.Equal("abc", welcome.GetProperty("buildId").GetString());
    }

    [Fact]
    public async Task Connect_BuildChangedOnDiskAfterStartup_WelcomesWithTheIdentifierReadAtStartup()
    {
        // Given
        WriteClientBuild("""{ "buildId": "first" }""");
        var factory = CreateFactory();
        await ConnectAndReadWelcomeAsync(factory);

        // When
        Write("build.json", """{ "buildId": "second" }""");
        var welcome = await ConnectAndReadWelcomeAsync(factory);

        // Then
        Assert.Equal(new Welcome("first"), welcome);
    }

    [Fact]
    public async Task Connect_EveryNewConnection_IsWelcomed()
    {
        // Given
        WriteClientBuild("""{ "buildId": "abc" }""");
        var factory = CreateFactory();
        await ConnectAndReadWelcomeAsync(factory);

        // When: a restored connection is a new one for the server.
        var welcome = await ConnectAndReadWelcomeAsync(factory);

        // Then
        Assert.Equal(new Welcome("abc"), welcome);
    }

    [Fact]
    public async Task Connect_NoClientBuild_WelcomesWithoutIdentifier()
    {
        // Given
        Directory.CreateDirectory(_webRoot.Path);
        var factory = CreateFactory();

        // When
        var welcome = await ConnectAndReadWelcomeAsync(factory);

        // Then
        Assert.Equal(new Welcome(BuildId: null), welcome);
    }

    [Fact]
    public async Task Connect_ClientBuildWithoutBuildFile_WelcomesWithoutIdentifierAndWarnsAtStartup()
    {
        // Given
        WriteClientBuild(buildFile: null);
        var factory = CreateFactory();

        // When
        var welcome = await ConnectAndReadWelcomeAsync(factory);

        // Then
        Assert.Equal(new Welcome(BuildId: null), welcome);
        var warning = Assert.Single(LoggedEvent.ReadAll(_logs), e => e.Template.StartsWith("Client build has no", StringComparison.Ordinal));
        Assert.Equal("Warning", warning.Level);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("{}")]
    [InlineData("""{ "buildId": 42 }""")]
    [InlineData("""{ "buildId": "  " }""")]
    [InlineData("""["abc"]""")]
    public async Task Connect_UnreadableBuildFile_WelcomesWithoutIdentifierAndWarnsAtStartup(string buildFile)
    {
        // Given
        WriteClientBuild(buildFile);
        var factory = CreateFactory();

        // When
        var welcome = await ConnectAndReadWelcomeAsync(factory);

        // Then
        Assert.Equal(new Welcome(BuildId: null), welcome);
        var warning = Assert.Single(LoggedEvent.ReadAll(_logs), e => e.Template.StartsWith("Client build identifier unreadable", StringComparison.Ordinal));
        Assert.Equal("Warning", warning.Level);
    }

    [Fact]
    public async Task Startup_BuildIdentified_LogsTheBuildServed()
    {
        // Given
        WriteClientBuild("""{ "buildId": "abc" }""");

        // When
        var factory = CreateFactory();
        await ConnectAndReadWelcomeAsync(factory);

        // Then
        var served = Assert.Single(LoggedEvent.ReadAll(_logs), e => e.Template == "Serving client build {BuildId}");
        Assert.Contains("\"BuildId\":\"abc\"", served.Line, StringComparison.Ordinal);
    }

    private static async Task<Welcome> ConnectAndReadWelcomeAsync(WebApplicationFactory<Program> factory)
    {
        var received = new TaskCompletionSource<Welcome>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var connection = await HubClients.ConnectAsync(
            factory,
            beforeStart: c => c.On<Welcome>(nameof(IGameClient.ReceiveWelcome), received.SetResult));
        return await received.Task.WaitAsync(TimeSpan.FromSeconds(10), Ct);
    }

    private WebApplicationFactory<Program> CreateFactory()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder
            .UseSetting(WebHostDefaults.WebRootKey, _webRoot.Path)
            .UseScratchDirectory(_logs.Path));
        return _factory;
    }

    private void WriteClientBuild(string? buildFile)
    {
        Write("index.html", "player page");
        if (buildFile is not null)
        {
            Write("build.json", buildFile);
        }
    }

    private void Write(string relativePath, string content)
    {
        var path = Path.Combine(_webRoot.Path, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }
}
