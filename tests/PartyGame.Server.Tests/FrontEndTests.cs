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
    [InlineData("/diagnostic/", "diagnostic page")]
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
    public async Task Get_BuildFile_MustBeRevalidated()
    {
        WriteClientBuild();
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/build.json", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
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

    [Theory]
    [InlineData("/display", "/display/")]
    [InlineData("/gm", "/gm/")]
    [InlineData("/Display", "/display/")]
    [InlineData("/DISPLAY/", "/display/")]
    [InlineData("/GM/", "/gm/")]
    [InlineData("/Gm", "/gm/")]
    [InlineData("/display?code=123456", "/display/?code=123456")]
    [InlineData("/diagnostic", "/diagnostic/")]
    [InlineData("/Diagnostic/", "/diagnostic/")]
    public async Task Get_PageWithoutTrailingSlashOrInAnotherCase_RedirectsTemporarilyToCanonicalPage(string path, string location)
    {
        WriteClientBuild();
        await using var factory = CreateFactory();
        using var client = CreateNonRedirectingClient(factory);

        using var response = await GetHtmlAsync(client, path);

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal(location, response.Headers.Location?.OriginalString);
    }

    [Theory]
    [InlineData("/display", "display page")]
    [InlineData("/GM", "gm page")]
    public async Task Get_PageAddressMistypedInBrowser_EndsOnThatPage(string path, string content)
    {
        WriteClientBuild();
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        using var response = await GetHtmlAsync(client, path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(content, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData("/rejoindre")]
    [InlineData("/index")]
    [InlineData("/display/unknown")]
    [InlineData("/joueur/?x=1")]
    public async Task Get_UnknownPageFromBrowser_RedirectsTemporarilyToPlayerPage(string path)
    {
        WriteClientBuild();
        await using var factory = CreateFactory();
        using var client = CreateNonRedirectingClient(factory);

        using var response = await GetHtmlAsync(client, path);

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal("/", response.Headers.Location?.OriginalString);
    }

    [Theory]
    [InlineData("/api/unknown")]
    [InlineData("/hub")]
    [InlineData("/hub/unknown")]
    [InlineData("/media/missing.mp3")]
    [InlineData("/assets")]
    [InlineData("/assets/missing-AbCd1234.js")]
    [InlineData("/health/unknown")]
    [InlineData("/API/unknown")]
    public async Task Get_UnknownPathUnderReservedPrefix_Answers404WithoutRedirect(string path)
    {
        WriteClientBuild();
        await using var factory = CreateFactory();
        using var client = CreateNonRedirectingClient(factory);

        using var response = await GetHtmlAsync(client, path);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Null(response.Headers.Location);
    }

    [Fact]
    public async Task Get_UnknownPathNotAskingForHtml_Answers404()
    {
        WriteClientBuild();
        await using var factory = CreateFactory();
        using var client = CreateNonRedirectingClient(factory);
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("/rejoindre", UriKind.Relative));
        request.Headers.Accept.ParseAdd("application/json");

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Post_UnknownPath_IsNotRedirected()
    {
        WriteClientBuild();
        await using var factory = CreateFactory();
        using var client = CreateNonRedirectingClient(factory);
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri("/rejoindre", UriKind.Relative));
        request.Headers.Accept.ParseAdd("text/html");

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_HealthEndpointFromBrowser_IsNotRedirected()
    {
        WriteClientBuild();
        await using var factory = CreateFactory();
        using var client = CreateNonRedirectingClient(factory);

        using var response = await GetHtmlAsync(client, "/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
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

    [Theory]
    [InlineData("/")]
    [InlineData("/rejoindre")]
    public async Task Get_PageWhileClientBuildMissing_Answers404WithoutRedirect(string path)
    {
        Directory.CreateDirectory(_webRoot.Path);
        await using var factory = CreateFactory();
        using var client = CreateNonRedirectingClient(factory);

        using var response = await GetHtmlAsync(client, path);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Null(response.Headers.Location);
    }

    private static HttpClient CreateNonRedirectingClient(WebApplicationFactory<Program> factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    // What a browser sends when the user types an address.
    private static async Task<HttpResponseMessage> GetHtmlAsync(HttpClient client, string path)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(path, UriKind.Relative));
        request.Headers.Accept.ParseAdd("text/html,application/xhtml+xml,*/*;q=0.8");
        return await client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    private WebApplicationFactory<Program> CreateFactory() =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder
            .UseSetting(WebHostDefaults.WebRootKey, _webRoot.Path)
            .UseScratchDirectory(_logs.Path));

    private void WriteClientBuild()
    {
        Write("index.html", "player page");
        Write("display/index.html", "display page");
        Write("gm/index.html", "gm page");
        Write("diagnostic/index.html", "diagnostic page");
        Write("assets/player-Bz1c35Cc.js", "console.log('player');");
        Write("build.json", """{ "buildId": "abc" }""");
    }

    private void Write(string relativePath, string content)
    {
        var path = Path.Combine(_webRoot.Path, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }
}
