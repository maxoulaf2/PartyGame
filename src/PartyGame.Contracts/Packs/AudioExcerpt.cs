using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace PartyGame.Contracts.Packs;

/// <summary>
/// An excerpt of an MP3 file of a pack, played on the TV screen: from a point of the track, for a given time. Shared by
/// every game mode that plays audio.
/// </summary>
/// <remarks>
/// The loading of the pack reads the duration of the file and refuses an excerpt that starts past its end. An excerpt that
/// runs past the end is accepted: it stops with the track.
/// </remarks>
[Description("Extrait d'un fichier MP3 du pack, joué sur l'écran TV : à partir d'un point du morceau, pendant une durée donnée.")]
public sealed record AudioExcerpt
{
    /// <summary>
    /// The latest point an excerpt may start from, in seconds.
    /// </summary>
    public const double MaxStartSeconds = 3600;

    /// <summary>
    /// The shortest excerpt, in seconds.
    /// </summary>
    public const int MinDurationSeconds = 5;

    /// <summary>
    /// The longest excerpt, in seconds.
    /// </summary>
    public const int MaxDurationSeconds = 120;

    /// <summary>
    /// The MP3 file of the track.
    /// </summary>
    [AudioFile]
    [Description("Fichier du morceau : chemin d'un fichier .mp3 du pack.")]
    public required MediaPath File { get; init; }

    /// <summary>
    /// The point of the track the excerpt starts from, in seconds. It must be before the end of the track.
    /// </summary>
    [Range(0, MaxStartSeconds)]
    [Description("Point de départ de l'extrait dans le morceau, en secondes, décimales permises, avant la fin du morceau. 0 si absent.")]
    public double Start { get; init; }

    /// <summary>
    /// The time the excerpt plays, in seconds.
    /// </summary>
    [Range(MinDurationSeconds, MaxDurationSeconds)]
    [Description("Durée de l'extrait, en secondes, de 5 à 120. Un extrait qui dépasse la fin du morceau s'arrête avec lui.")]
    public required int Duration { get; init; }
}
