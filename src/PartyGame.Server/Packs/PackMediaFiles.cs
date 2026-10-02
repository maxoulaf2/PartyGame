using Microsoft.AspNetCore.StaticFiles;
using PartyGame.Engine;
using PartyGame.Server.Games;

namespace PartyGame.Server.Packs;

/// <summary>
/// Serves the media files of the pack the game plays, by the identifiers drawn when the game started. The file served
/// comes from the state only, never from the URL: no request can reach another file of the disk, nor a file of another
/// pack, nor a media file before the game starts.
/// </summary>
internal sealed class PackMediaFiles(GameLoop game, ILogger<PackMediaFiles> logger)
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

        if (Find(game.State, new MediaId(id)) is not { } file)
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
        return Results.File(file, contentType, lastModified: File.GetLastWriteTimeUtc(file), enableRangeProcessing: true);
    }

    /// <summary>
    /// The full path of the media file an identifier designates in the pack of the game.
    /// </summary>
    /// <returns>The path, or <see langword="null"/> when the game has no media file with this identifier.</returns>
    internal static string? Find(GameState state, MediaId id)
    {
        if (state is not { Pack: not null, SelectedPackId: { } packId } || state.Media.Find(id) is not { } media)
        {
            return null;
        }

        var folder = Path.GetFullPath(Path.Combine(state.Catalog.Directory, packId));
        var file = Path.GetFullPath(Path.Combine([folder, .. media.Value.Split('/')]));

        // The loading refuses any path out of the pack. Checked again all the same: a file out of it would expose the disk.
        return file.StartsWith(folder + Path.DirectorySeparatorChar, StringComparison.Ordinal) ? file : null;
    }
}
