using System.Collections.Immutable;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace PartyGame.Contracts.Packs;

/// <summary>
/// A blind test round: the TV screen plays an excerpt of each track, the first player to buzz names the title and the
/// artist aloud, and the game master judges each of them.
/// </summary>
[Description("Manche de blind test : l'écran TV joue un extrait de chaque morceau, le premier joueur qui buzze donne le titre et l'artiste à voix haute, et le game master juge chacun des deux.")]
public sealed record BlindTestRoundDescriptor : RoundDescriptor
{
    /// <summary>
    /// The points of the title, or of the artist, when the round does not set them.
    /// </summary>
    public const int DefaultPoints = 500;

    /// <summary>
    /// The highest points of the title, or of the artist.
    /// </summary>
    public const int MaxPoints = 10_000;

    /// <summary>
    /// The points of the player who finds the title of a track.
    /// </summary>
    [Range(0, MaxPoints)]
    [Description("Points de qui trouve le titre d'un morceau, de 0 à 10 000. 500 si absent.")]
    public int TitlePoints { get; init; } = DefaultPoints;

    /// <summary>
    /// The points of the player who finds the artist of a track. A track without an artist is played on its title only.
    /// </summary>
    [Range(0, MaxPoints)]
    [Description("Points de qui trouve l'artiste d'un morceau, de 0 à 10 000. 500 si absent. Un morceau sans artiste ne se joue que sur le titre.")]
    public int ArtistPoints { get; init; } = DefaultPoints;

    /// <summary>
    /// The tracks of the round, played in this order.
    /// </summary>
    [Length(1, 50)]
    [Description("Morceaux de la manche, de 1 à 50, joués dans cet ordre.")]
    public required ImmutableArray<BlindTestTrack> Tracks { get; init; }
}
