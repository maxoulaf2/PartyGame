namespace PartyGame.Contracts.BlindTest;

/// <summary>
/// What a blind test round shows on the TV screen: the excerpt it plays, then who has the hand and who found what, and the
/// track at the reveal. Never the title nor the artist before the reveal, nor the time stamps of the buzzes.
/// </summary>
/// <param name="TrackNumber">The track in progress, from 1.</param>
/// <param name="TrackCount">How many tracks the round has.</param>
/// <param name="Phase">Phase of the track in progress.</param>
/// <param name="Playback">The excerpt of the track: preloaded, playing or paused.</param>
/// <param name="Winner">The nickname of the player who has the hand, or <see langword="null"/>.</param>
/// <param name="TitleFoundBy">The nickname of the player who found the title, or <see langword="null"/>.</param>
/// <param name="ArtistFoundBy">The nickname of the player who found the artist, or <see langword="null"/>.</param>
/// <param name="Title">The title of the track, once revealed, or <see langword="null"/> before.</param>
/// <param name="Artist">
/// The artist of the track, once revealed, or <see langword="null"/> before and for a track without artist.
/// </param>
/// <param name="ImageUrl">The URL of the image of the track, once revealed, or <see langword="null"/>.</param>
public sealed record BlindTestDisplayView(
    int TrackNumber,
    int TrackCount,
    BlindTestTrackPhase Phase,
    AudioPlayback Playback,
    string? Winner,
    string? TitleFoundBy,
    string? ArtistFoundBy,
    string? Title,
    string? Artist,
    string? ImageUrl) : DisplayRoundView;
