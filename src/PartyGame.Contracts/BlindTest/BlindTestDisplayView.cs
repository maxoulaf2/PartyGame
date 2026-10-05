namespace PartyGame.Contracts.BlindTest;

/// <summary>
/// What a blind test round shows on the TV screen: the excerpt it plays, then who has the hand. Never the title nor the
/// artist before the reveal, nor the time stamps of the buzzes.
/// </summary>
/// <param name="TrackNumber">The track in progress, from 1.</param>
/// <param name="TrackCount">How many tracks the round has.</param>
/// <param name="Phase">Phase of the track in progress.</param>
/// <param name="Playback">The excerpt of the track: preloaded, playing or paused.</param>
/// <param name="Winner">The nickname of the player who has the hand, or <see langword="null"/>.</param>
public sealed record BlindTestDisplayView(
    int TrackNumber,
    int TrackCount,
    BlindTestTrackPhase Phase,
    AudioPlayback Playback,
    string? Winner) : DisplayRoundView;
