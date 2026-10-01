using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace PartyGame.Server.Tests;

public sealed class FrontEndTests : IDisposable
{
    private readonly TempDirectory _webRoot = new();
    private readonly TempDirectory _logs = new();

    public void Dispose()
    {
        _webRoot.Dispose();
        _logs.Dispose();
    }

    [Theory]
    [InlineData("/", "player page")]
    [InlineData("/display/", "display page")]
    [InlineData("/gm/", "gm page")]
    public async Task Get_PageOfClientBuild_ServesHtmlThatMustBeRevalidated(string path, string content)
    {
        WriteClientBuild();
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(new Uri(path, UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(content, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.True(response.Headers.CacheControl?.NoCache);
    }

    [Fact]
    public async Task Get_FingerprintedAsset_IsCachedImmutably()
    {
        WriteClientBuild();
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/assets/player-Bz1c35Cc.js", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(TimeSpan.FromDays(365), response.Headers.CacheControl?.MaxAge);
        Assert.Contains(response.Headers.CacheControl!.Extensions, header => header.Name == "immutable");
    }

    [Fact]
    public async Task Startup_ClientBuildMissing_LogsWarningAndAnswers404()
    {
        Directory.CreateDirectory(_webRoot.Path);
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("npm run build", _logs.ReadAllLogs(), StringComparison.Ordinal);
    }

    private WebApplicationFactory<Program> CreateFactory() =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder
            .UseSetting(WebHostDefaults.WebRootKey, _webRoot.Path)
            .UseSetting("LogFiles:Directory", _logs.Path));

    private void WriteClientBuild()
    {
        Write("index.html", "player page");
        Write("display/index.html", "display page");
        Write("gm/index.html", "gm page");
        Write("assets/player-Bz1c35Cc.js", "console.log('player');");
    }

    private void Write(string relativePath, string content)
    {
        var path = Path.Combine(_webRoot.Path, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }
}
