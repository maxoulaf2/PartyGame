using System.Text.Json.Serialization;
using PartyGame.Contracts;
using PartyGame.Contracts.Packs;
using PartyGame.Engine.Audio;

namespace PartyGame.Engine.Modes.BlindTest;

/// <summary>
/// State of a blind test round: the track in progress, the playback of its excerpt, its buzzer, and who found what.
/// </summary>
/// <param name="Descriptor">The activity of the pack the round plays, for its tracks.</param>
/// <param name="TrackIndex">Position of the track in progress in <paramref name="Descriptor"/>, from 0.</param>
public sealed record BlindTestRound(BlindTestRoundDescriptor Descriptor, int TrackIndex) : RoundState
{
    /// <summary>
    /// The track in progress.
    /// </summary>
    [JsonIgnore] // read from the descriptor, which is persisted
    public BlindTestTrack Track => Descriptor.Tracks[TrackIndex];

    /// <summary>
    /// The number of the track in progress, from 1, as the screens show it and the intents name it.
    /// </summary>
    [JsonIgnore] // read from the index, which is persisted
    public int TrackNumber => TrackIndex + 1;

    /// <summary>
    /// The playback of the excerpt of the track in progress: standing still at its start until the game master plays it.
    /// </summary>
    public ExcerptPlayback Playback { get; init; } = ExcerptPlayback.Ready(Descriptor.Tracks[TrackIndex].Excerpt);

    /// <summary>
    /// The buzzer of the track in progress: closed until the game master plays its excerpt, a fresh one for each track.
    /// </summary>
    public Buzzers.Buzzer Buzzer { get; init; } = new();

    /// <summary>
    /// The player whose answer gave the title of the track in progress, or <see langword="null"/>.
    /// </summary>
    public PlayerId? TitleFoundBy { get; init; }

    /// <summary>
    /// The player whose answer gave the artist of the track in progress, or <see langword="null"/>.
    /// </summary>
    public PlayerId? ArtistFoundBy { get; init; }

    /// <summary>
    /// Whether the title, or the artist of a track that has one, is still to find.
    /// </summary>
    [JsonIgnore] // derived from the track and who found what, which are persisted
    public bool IsSomethingLeft => TitleFoundBy is null || (Track.Artist is not null && ArtistFoundBy is null);

    /// <summary>
    /// Phase of the track in progress: the reveal closes the buzzer until the next track.
    /// </summary>
    [JsonIgnore] // derived from the buzzer, which is persisted
    public BlindTestPhase Phase =>
        Buzzer.Opening == 0 ? BlindTestPhase.Ready
        : Buzzer.IsClosed ? BlindTestPhase.Revealed
        : Buzzer.Winner is not null ? BlindTestPhase.Answering
        : Buzzer.ArbitrateAt is not null ? BlindTestPhase.Arbitrating
        : BlindTestPhase.Listening;
}
