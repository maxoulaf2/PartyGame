using System.Text.Json.Serialization;
using PartyGame.Contracts.Packs;
using PartyGame.Engine.Audio;

namespace PartyGame.Engine.Modes.BlindTest;

/// <summary>
/// State of a blind test round: the track in progress, the playback of its excerpt, and its buzzer.
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
    /// Phase of the track in progress.
    /// </summary>
    [JsonIgnore] // derived from the buzzer, which is persisted
    public BlindTestPhase Phase =>
        Buzzer.Opening == 0 ? BlindTestPhase.Ready
        : Buzzer.Winner is not null ? BlindTestPhase.Answering
        : Buzzer.ArbitrateAt is not null ? BlindTestPhase.Arbitrating
        : BlindTestPhase.Listening;
}
