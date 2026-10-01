using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using PartyGame.Server.GameMaster;

namespace PartyGame.Server.Tests.GameMaster;

public sealed class GameMasterCodeExposureTests : IDisposable
{
    private const string Code = "482913";

    private readonly TempDirectory _webRoot = new();
    private readonly TempDirectory _logs = new();

    public void Dispose()
    {
        _webRoot.Dispose();
        _logs.Dispose();
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/display/")]
    [InlineData("/gm/")]
    [InlineData("/api/join")]
    [InlineData("/health")]
    public async Task Get_AnyServedResponse_NeverContainsTheCode(string path)
    {
        WritePage("index.html");
        WritePage("display/index.html");
        WritePage("gm/index.html");
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(new Uri(path, UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.True(response.IsSuccessStatusCode);
        Assert.DoesNotContain(Code, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), StringComparison.Ordinal);
        Assert.DoesNotContain(response.Headers, header => header.Value.Any(value => value.Contains(Code, StringComparison.Ordinal)));
    }

    [Fact]
    public async Task Startup_CodeConfigured_IsTheCodeVerified()
    {
        await using var factory = CreateFactory();

        var code = factory.Services.GetRequiredService<GameMasterCode>();

        Assert.True(code.Verify(Code));
        Assert.True(code.IsConfigured);
    }

    private WebApplicationFactory<Program> CreateFactory() =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder
            .UseSetting(Microsoft.AspNetCore.Hosting.WebHostDefaults.WebRootKey, _webRoot.Path)
            .UseSetting("LogFiles:Directory", _logs.Path)
            .UseSetting("GameMaster:Code", Code));

    // A page that loads the client like the real build does, without the code anywhere in it.
    private void WritePage(string relativePath)
    {
        var path = Path.Combine(_webRoot.Path, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "<!doctype html><script type=\"module\" src=\"/assets/app.js\"></script>");
    }
}
