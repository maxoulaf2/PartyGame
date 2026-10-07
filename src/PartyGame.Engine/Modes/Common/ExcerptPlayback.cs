using System.Text.Json.Serialization;
using PartyGame.Contracts;
using PartyGame.Contracts.Packs;

namespace PartyGame.Engine.Modes.Common;

/// <summary>
/// The playback of an excerpt on the TV screen, which a game mode keeps in the state of its round and projects to the TV
/// screen as an <see cref="AudioPlayback"/>: the TV screen plays what it describes, the engine never sends it an order.
/// Like the rest of the engine, it is pure.
/// </summary>
/// <param name="Position">
/// The position in the file, in seconds, the playback starts from at <paramref name="StartsAt"/>, or where it stands
/// while it does not play.
/// </param>
/// <param name="StartsAt">
/// When the TV screen plays from <paramref name="Position"/>, in server time, or <see langword="null"/> while the
/// playback stands still.
/// </param>
public sealed record ExcerptPlayback(double Position, DateTimeOffset? StartsAt)
{
    /// <summary>
    /// How long after its decision a playback starts: time for the TV screen to receive its snapshot and to set the
    /// preloaded file to the right position (decision 4 of E14).
    /// </summary>
    public static readonly TimeSpan Lead = TimeSpan.FromMilliseconds(500);

    /// <summary>
    /// Whether the playback plays, or is about to.
    /// </summary>
    [JsonIgnore] // derived from the start, which is persisted
    public bool IsPlaying => StartsAt is not null;

    /// <summary>
    /// The playback of an excerpt, standing still at its start until it plays.
    /// </summary>
    /// <param name="excerpt">The excerpt to play.</param>
    public static ExcerptPlayback Ready(AudioExcerpt excerpt)
    {
        ArgumentNullException.ThrowIfNull(excerpt);
        return new(excerpt.Start, null);
    }

    /// <summary>
    /// The position in the file the excerpt ends at, in seconds.
    /// </summary>
    /// <param name="excerpt">The excerpt played.</param>
    public static double EndOf(AudioExcerpt excerpt)
    {
        ArgumentNullException.ThrowIfNull(excerpt);
        return excerpt.Start + excerpt.Duration;
    }

    /// <summary>
    /// Plays from the current position, <see cref="Lead"/> from now. A playback that plays already goes on.
    /// </summary>
    /// <param name="now">Current server time.</param>
    public ExcerptPlayback Play(DateTimeOffset now) => IsPlaying ? this : this with { StartsAt = now + Lead };

    /// <summary>
    /// Stands still where the playback is now, at most at the end of the excerpt: played again, it goes on from there.
    /// </summary>
    /// <param name="now">Current server time.</param>
    /// <param name="end">The position in the file the excerpt ends at, in seconds.</param>
    public ExcerptPlayback Pause(DateTimeOffset now, double end) =>
        StartsAt is { } startsAt ? new(Math.Min(end, Position + Math.Max(0, (now - startsAt).TotalSeconds)), null) : this;

    /// <summary>
    /// Resumes the playback of a game saved before the server stopped: it goes on from where it was at the last save.
    /// </summary>
    /// <param name="shift">The time spent offline: now minus the time of the last save.</param>
    public ExcerptPlayback Resume(TimeSpan shift) => this with { StartsAt = StartsAt + shift };

    /// <summary>
    /// What the TV screen receives of the playback.
    /// </summary>
    /// <param name="url">The URL the file is served at.</param>
    /// <param name="end">The position in the file the excerpt ends at, in seconds.</param>
    public AudioPlayback Project(string url, double end) => new(url, Position, end, StartsAt?.ToUnixTimeMilliseconds());
}
