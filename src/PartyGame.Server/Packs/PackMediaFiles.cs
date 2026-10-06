using System.Collections.Immutable;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Options;
using PartyGame.Content;
using PartyGame.Contracts.Packs;
using PartyGame.Engine;
using PartyGame.Server.Games;
using PartyGame.Server.Persistence;

namespace PartyGame.Server.Packs;

/// <summary>
/// Serves the media files of the pack the game plays, by the identifiers drawn when the game started. The file served
/// comes from the state only, never from the URL: no request can reach another file of the disk, nor a file of another
/// pack, nor a media file before the game starts.
/// </summary>
internal sealed class PackMediaFiles(GameLoop game, IOptions<PersistenceOptions> persistence, ILogger<PackMediaFiles> logger)
{
    // An identifier designates the same file for the whole game: the TV screen loads each one once.
    private const string CacheControl = "private, max-age=86400, immutable";

    private const string DefaultContentType = "application/octet-stream";

    private static readonly FileExtensionContentTypeProvider _contentTypes = new();

    /// <summary>
    /// The media file an identifier designates, as a full response or as the range the request asks for.
    /// </summary>
    /// <param name="id">The identifier, as received in the URL.</param>
    /// <param name="response">The response, which gets its cache headers when the file is found.</param>
    /// <returns>The file, or a 404 when the game has no media file with this identifier.</returns>
    public IResult Serve(string id, HttpResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);

        if (Find(game.State, new MediaId(id), persistence.Value.FullPackCacheDirectory) is not { } file)
        {
            return Results.NotFound();
        }

        if (!File.Exists(file))
        {
            // Checked when the pack was loaded: the file was deleted or moved during the game. The TV screen shows the
            // question without its image.
            logger.PackMediaFileMissing(file);
            return Results.NotFound();
        }

        response.Headers.CacheControl = CacheControl;
        var contentType = _contentTypes.TryGetContentType(file, out var type) ? type : DefaultContentType;
        var lastModified = File.GetLastWriteTimeUtc(file);
        if (contentType == "audio/mpeg")
        {
            return ServeAudio(file, contentType, lastModified);
        }

        return Results.File(file, contentType, lastModified: lastModified, enableRangeProcessing: true);
    }

    /// <summary>
    /// An MP3 file without its ID3 tags, as if they had never been there: its title, artist and cover would give away the
    /// answer of a blind test to whoever downloads it from the TV screen.
    /// </summary>
    internal static IResult ServeAudio(string file, string contentType, DateTimeOffset lastModified)
    {
        var stream = File.OpenRead(file);
        try
        {
            var audio = Mp3Audio.Find(stream);
            return Results.File(
                new FileSegmentStream(stream, audio.Start, audio.Length),
                contentType,
                lastModified: lastModified,
                enableRangeProcessing: true);
        }
        catch
        {
            stream.Dispose();
            throw;
        }
    }

    /// <summary>
    /// The media files of the pack of a game missing from the disk, as the server would fail to serve them, in the ordinal
    /// order of their path: a game is resumed only once none is.
    /// </summary>
    internal static ImmutableArray<MediaPath> Missing(GameState state, string cacheDirectory) =>
        [.. state.Media.Files
            .Where(media => Find(state, media.Key, cacheDirectory) is not { } file || !File.Exists(file))
            .Select(media => media.Value)
            .OrderBy(media => media.Value, StringComparer.Ordinal)];

    /// <summary>
    /// The full path of the media file an identifier designates in the pack of the game: in its folder of the pack
    /// directory, or else in the cache the zip packs are extracted to.
    /// </summary>
    /// <returns>The path, or <see langword="null"/> when the game has no media file with this identifier.</returns>
    internal static string? Find(GameState state, MediaId id, string cacheDirectory)
    {
        if (state is not { Pack: not null, SelectedPackId: { } packId } || state.Media.Find(id) is not { } media)
        {
            return null;
        }

        // The loading never lets a folder and a zip with the same identifier be chosen: the one holding pack.json is it.
        var folder = Path.GetFullPath(Path.Combine(state.Catalog.Directory, packId));
        if (!File.Exists(Path.Combine(folder, PackDescriptor.FileName)))
        {
            folder = Path.GetFullPath(Path.Combine(cacheDirectory, packId));
        }

        var file = Path.GetFullPath(Path.Combine([folder, .. media.Value.Split('/')]));

        // The loading refuses any path out of the pack. Checked again all the same: a file out of it would expose the disk.
        return file.StartsWith(folder + Path.DirectorySeparatorChar, StringComparison.Ordinal) ? file : null;
    }
}
