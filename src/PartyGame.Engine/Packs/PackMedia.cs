using System.Buffers.Text;
using System.Collections.Immutable;
using PartyGame.Contracts.Packs;

namespace PartyGame.Engine.Packs;

/// <summary>
/// The media files of the pack the game plays, each under an identifier drawn at random when the game starts. The server
/// serves a file only by its identifier, so the URL a client sees never reveals which file it is. Part of the state, so
/// that the identifiers survive a resumption after a crash; no projection ever contains the paths.
/// </summary>
/// <param name="Files">The path of each media file, as written in the descriptor, by its identifier.</param>
public sealed record PackMedia(ImmutableDictionary<MediaId, MediaPath> Files)
{
    /// <summary>
    /// The URL prefix the server serves the media files under, followed by their identifier.
    /// </summary>
    public const string UrlPrefix = "/media";

    // 128 bits: no client can guess the identifier of a file it was not shown.
    private const int IdBytes = 16;

    /// <summary>
    /// No media file: the game has not started yet.
    /// </summary>
    public static PackMedia Empty { get; } = new(ImmutableDictionary<MediaId, MediaPath>.Empty);

    /// <summary>
    /// Draws an identifier for each media file of a pack.
    /// </summary>
    /// <param name="media">The media files of the pack, each path once.</param>
    /// <param name="random">The random generator of the context, so that a replayed game gets the same identifiers.</param>
    public static PackMedia Draw(IEnumerable<MediaPath> media, Random random)
    {
        ArgumentNullException.ThrowIfNull(media);
        ArgumentNullException.ThrowIfNull(random);

        var files = ImmutableDictionary.CreateBuilder<MediaId, MediaPath>();
        Span<byte> bytes = stackalloc byte[IdBytes];
        foreach (var path in media)
        {
            random.NextBytes(bytes);
            files.Add(new MediaId(Base64Url.EncodeToString(bytes)), path);
        }

        return new PackMedia(files.ToImmutable());
    }

    /// <summary>
    /// Finds the media file an identifier designates.
    /// </summary>
    /// <param name="id">The identifier, as received in a URL.</param>
    /// <returns>The path of the file in the pack, or <see langword="null"/> when the game has no media with this identifier.</returns>
    public MediaPath? Find(MediaId id) => Files.TryGetValue(id, out var path) ? path : null;

    /// <summary>
    /// The URL a client loads a media file of the pack from, the only reference to it a projection may contain.
    /// </summary>
    /// <param name="path">The path of the file, as written in the descriptor of the pack the game plays.</param>
    /// <exception cref="InvalidOperationException">
    /// The pack has no such media file: a bug, since the loading of the pack lists every media file it references.
    /// </exception>
    public string UrlOf(MediaPath path)
    {
        foreach (var (id, file) in Files)
        {
            if (file == path)
            {
                return $"{UrlPrefix}/{id.Value}";
            }
        }

        throw new InvalidOperationException("The pack of the game has no such media file.");
    }
}
