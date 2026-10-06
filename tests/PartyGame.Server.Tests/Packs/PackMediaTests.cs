using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using PartyGame.Contracts;
using PartyGame.Contracts.Packs;
using PartyGame.Engine;
using PartyGame.Server.Games;
using PartyGame.Server.Hubs;
using PartyGame.Server.Packs;
using PartyGame.Server.Tests.Hubs;
using PartyGame.Tests.Shared.Leaks;

namespace PartyGame.Server.Tests.Packs;

public sealed class PackMediaTests : IAsyncDisposable
{
    private const string Code = "482913";

    private const string Flag = "images/drapeau-japon.png";

    private const string Monument = "monuments/tour-eiffel.webp";

    private const string Portrait = "portrait.jpg";

    private const string NeighbourImage = "images/voisin.png";

    private static readonly byte[] _flagContent = [.. Enumerable.Range(0, 100).Select(i => (byte)i)];

    private readonly TempDirectory _logs = new();
    private readonly TempDirectory _packs = new();
    private readonly WebApplicationFactory<Program> _factory;

    public PackMediaTests()
    {
        // Two valid packs with images, so that the one played has to be chosen.
        TestPacks.Write(_packs.Path, "soiree", TestPacks.IllustratedQuiz("Grande soirée", Flag, Monument, Portrait, Flag));
        TestPacks.WriteMedia(_packs.Path, "soiree", Flag, _flagContent);
        TestPacks.WriteMedia(_packs.Path, "soiree", Monument, [1, 2, 3]);
        TestPacks.WriteMedia(_packs.Path, "soiree", Portrait, [4, 5, 6]);
        TestPacks.Write(_packs.Path, "voisin", TestPacks.IllustratedQuiz("Pack voisin", NeighbourImage));
        TestPacks.WriteMedia(_packs.Path, "voisin", NeighbourImage, [7, 8, 9]);

        // A third one shared as a zip, whose media files are served from the cache it is extracted to.
        TestPacks.Write(_packs.Path, "album", TestPacks.IllustratedQuiz("Album", Flag));
        TestPacks.WriteMedia(_packs.Path, "album", Flag, _flagContent);
        ZipFile.CreateFromDirectory(Path.Combine(_packs.Path, "album"), Path.Combine(_packs.Path, "album.zip"));
        Directory.Delete(Path.Combine(_packs.Path, "album"), recursive: true);
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder
            .UseScratchDirectory(_logs.Path)
            .UseSetting("GameMaster:Code", Code)
            .UseSetting(PacksOptions.DirectorySetting, _packs.Path));
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private GameLoop Game => _factory.Services.GetRequiredService<GameLoop>();

    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
        _logs.Dispose();
        _packs.Dispose();
    }

    [Theory]
    [InlineData(Flag, "image/png")]
    [InlineData(Monument, "image/webp")]
    [InlineData(Portrait, "image/jpeg")]
    public async Task Get_MediaOfTheGame_ServesTheWholeFileWithItsContentType(string media, string contentType)
    {
        // Given
        await StartGameAsync("soiree");
        using var client = CreateClient();

        // When
        using var response = await client.GetAsync(UrlOf(media), Ct);

        // Then
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(contentType, response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(File.ReadAllBytes(Path.Combine([_packs.Path, "soiree", .. media.Split('/')])), await response.Content.ReadAsByteArrayAsync(Ct));
        Assert.Contains("bytes", response.Headers.AcceptRanges);
        Assert.True(response.Headers.CacheControl?.Private);
    }

    [Fact]
    public async Task Get_RangeOfAMediaOfAZipPack_ServesThePartAskedFromTheExtractedFile()
    {
        // Given
        await StartGameAsync("album");
        using var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, UrlOf(Flag));
        request.Headers.Range = new RangeHeaderValue(10, 19);

        // When
        using var response = await client.SendAsync(request, Ct);

        // Then
        Assert.Equal(HttpStatusCode.PartialContent, response.StatusCode);
        Assert.Equal(_flagContent[10..20], await response.Content.ReadAsByteArrayAsync(Ct));
        Assert.DoesNotContain("album", UrlOf(Flag), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Get_RangeOfAMedia_ServesThePartAsked()
    {
        // Given: an excerpt that starts in the middle of the file
        await StartGameAsync("soiree");
        using var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, UrlOf(Flag));
        request.Headers.Range = new RangeHeaderValue(10, 19);

        // When
        using var response = await client.SendAsync(request, Ct);

        // Then
        Assert.Equal(HttpStatusCode.PartialContent, response.StatusCode);
        Assert.Equal(new ContentRangeHeaderValue(10, 19, _flagContent.Length), response.Content.Headers.ContentRange);
        Assert.Equal(_flagContent[10..20], await response.Content.ReadAsByteArrayAsync(Ct));
    }

    [Theory]
    [InlineData("/media/inconnu")]
    [InlineData("/media/AAAAAAAAAAAAAAAAAAAAAA")]
    [InlineData("/media/drapeau-japon.png")]
    [InlineData("/media/images/drapeau-japon.png")]
    [InlineData("/media/images%2Fdrapeau-japon.png")]
    [InlineData("/media/..%2Fsoiree%2Fpack.json")]
    [InlineData("/media/soiree/pack.json")]
    [InlineData("/media/")]
    [InlineData("/media")]
    public async Task Get_UnknownIdentifierOrPathOfAFile_IsNotFoundWithoutAnyContent(string url)
    {
        // Given
        await StartGameAsync("soiree");
        using var client = CreateClient();

        // When
        using var response = await client.GetAsync(url, Ct);

        // Then: no redirect to a page, and nothing listed
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Empty(await response.Content.ReadAsByteArrayAsync(Ct));
    }

    [Theory]
    [InlineData(NeighbourImage)]
    [InlineData("voisin.png")]
    [InlineData("voisin/images/voisin.png")]
    public async Task Get_MediaOfAnotherPack_IsNotFound(string url)
    {
        // Given
        await StartGameAsync("soiree");
        using var client = CreateClient();

        // When
        using var response = await client.GetAsync($"/media/{url}", Ct);

        // Then: the game has identifiers for the files of its own pack only
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(
            [Flag, Monument, Portrait],
            Game.State.Media.Files.Values.Select(media => media.Value).Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task Get_IdentifierOfAPreviousGame_IsNotFound()
    {
        // Given: an identifier drawn for another game of the same pack, such as one the TV screen kept in its cache
        var previous = PackMedia.Draw([new MediaPath(Flag)], new Random(1)).Files.Keys.Single();
        await StartGameAsync("soiree");
        using var client = CreateClient();

        // When
        using var response = await client.GetAsync($"/media/{previous.Value}", Ct);

        // Then
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_MediaBeforeTheGameStarts_IsNotFound()
    {
        // Given: the pack is chosen, but the game has not started
        await using var gameMaster = await ConnectGameMasterAsync();
        Assert.Null((await SelectAsync(gameMaster, "soiree")).Refusal);
        using var client = CreateClient();

        // When
        using var response = await client.GetAsync($"/media/{Flag}", Ct);

        // Then
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Empty(Game.State.Media.Files);
    }

    [Fact]
    public async Task Get_MediaDeletedDuringTheGame_IsNotFoundAndLogged()
    {
        // Given
        await StartGameAsync("soiree");
        File.Delete(Path.Combine(_packs.Path, "soiree", "portrait.jpg"));
        using var client = CreateClient();

        // When
        using var response = await client.GetAsync(UrlOf(Portrait), Ct);

        // Then: the TV screen shows the question without its image
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains(
            LoggedEvent.ReadAll(_logs),
            e => e.Level == "Warning" && e.Template.StartsWith("Media file {File} of the pack of the game is missing", StringComparison.Ordinal));
    }

    [Fact]
    public async Task StartGame_IllustratedPack_ShowsNoPathOfAMediaInTheIdentifiersNorInTheSnapshots()
    {
        // Given
        await using var display = await HubClients.ConnectAsync(_factory);
        await using var zoe = await HubClients.ConnectAsync(_factory);
        using var toDisplay = new ReceivedSnapshots(display);
        using var toZoe = new ReceivedSnapshots(zoe);
        Assert.Null((await display.InvokeAsync<AnnouncementResult>(GameHub.Announce, new Announcement(Role.Display, null), Ct)).Refusal);

        // When
        await StartGameAsync("soiree", zoe);

        // Then
        await Task.WhenAll(FlushAsync(display), FlushAsync(zoe));
        string[] secrets = ["images", "drapeau", "monuments", "tour-eiffel", "portrait", ".png", ".jpg", ".webp", _packs.Path];
        LeakAssert.NoSecretReceived(Viewer.Display, toDisplay.Json, [.. secrets.Select(s => new Secret(s, Audience.Everyone))]);
        LeakAssert.NoSecretReceived(Viewer.PhoneOf("Zoé"), toZoe.Json, [.. secrets.Select(s => new Secret(s, Audience.Everyone))]);
        Assert.All(Game.State.Media.Files.Keys, id => Assert.All(secrets, secret => Assert.DoesNotContain(secret, id.Value, StringComparison.OrdinalIgnoreCase)));
    }

    private HttpClient CreateClient() => _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    private string UrlOf(string media) => Game.State.Media.UrlOf(new MediaPath(media));

    /// <summary>
    /// Chooses a pack, then starts the game with a player, who is <paramref name="player"/> when given.
    /// </summary>
    private async Task StartGameAsync(string packId, HubConnection? player = null)
    {
        await using var gameMaster = await ConnectGameMasterAsync();
        await using var zoe = player is null ? await HubClients.ConnectAsync(_factory) : null;
        Assert.Null((await SelectAsync(gameMaster, packId)).Refusal);
        var joined = await (player ?? zoe!).InvokeAsync<JoinResult>(GameHub.JoinGame, new JoinRequest("Zoé"), Ct);
        Assert.Null(joined.Refusal);
        var started = await gameMaster.InvokeAsync<StartGameResult>(GameHub.StartGame, Ct);
        Assert.Null(started.Refusal);
    }

    private async Task<HubConnection> ConnectGameMasterAsync()
    {
        var connection = await HubClients.ConnectAsync(_factory);
        var result = await connection.InvokeAsync<AnnouncementResult>(GameHub.Announce, new Announcement(Role.GameMaster, Code), Ct);
        Assert.Null(result.Refusal);
        return connection;
    }

    private static Task<SelectPackResult> SelectAsync(HubConnection connection, string packId) =>
        connection.InvokeAsync<SelectPackResult>(GameHub.SelectPack, new SelectPackRequest(packId), Ct);

    private Task FlushAsync(HubConnection connection) => HubClients.FlushAsync<GameHub>(_factory, connection);
}
