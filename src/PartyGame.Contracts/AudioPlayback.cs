namespace PartyGame.Contracts;

/// <summary>
/// The excerpt the TV screen plays, as the view of a game mode describes it: the TV screen tells from it at any time what
/// it must play, after a reload or a resumed game too. Only the TV screen receives it: phones never play sound.
/// </summary>
/// <param name="Url">The URL of the MP3 file, under <c>/media/</c>: an opaque identifier that tells nothing of the track.</param>
/// <param name="Position">
/// The position in the file, in seconds, the playback starts from at <paramref name="StartsAt"/>, or where it stands
/// while it does not play: before it starts, and paused.
/// </param>
/// <param name="End">The position in the file the excerpt ends at, in seconds: the TV screen stops there by itself.</param>
/// <param name="StartsAt">
/// When the TV screen plays from <paramref name="Position"/>, in server time, in milliseconds since the Unix epoch, or
/// <see langword="null"/> while the playback stands still at <paramref name="Position"/>.
/// </param>
public sealed record AudioPlayback(string Url, double Position, double End, long? StartsAt);
