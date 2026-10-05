using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace PartyGame.Contracts.Packs;

/// <summary>
/// A track of a blind test round: the excerpt played on the TV screen, and what the players must name.
/// </summary>
[Description("Morceau d'une manche de blind test : l'extrait joué sur l'écran TV, et ce que les joueurs doivent trouver.")]
public sealed record BlindTestTrack
{
    /// <summary>
    /// The excerpt played on the TV screen.
    /// </summary>
    [Description("Extrait joué sur l'écran TV.")]
    public required AudioExcerpt Excerpt { get; init; }

    /// <summary>
    /// The title of the track: shown to the game master, who judges the answers given aloud, then to everyone at the reveal.
    /// </summary>
    [StringLength(100, MinimumLength = 1)]
    [Description("Titre du morceau, affiché au game master qui juge les réponses données à voix haute, puis à tous à la révélation.")]
    public required string Title { get; init; }

    /// <summary>
    /// The artist of the track, if the players must name one too: shown like the title.
    /// </summary>
    [StringLength(100, MinimumLength = 1)]
    [Description("Artiste du morceau, affiché comme le titre. Facultatif : un morceau sans artiste ne se joue que sur le titre.")]
    public string? Artist { get; init; }

    /// <summary>
    /// An optional image, such as the cover of the album, shown on the TV screen at the reveal.
    /// </summary>
    [Description("Visuel facultatif, comme la pochette de l'album, affiché sur l'écran TV à la révélation : chemin d'un fichier .jpg, .jpeg, .png ou .webp du pack.")]
    public MediaPath? Image { get; init; }
}
