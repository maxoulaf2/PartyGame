namespace PartyGame.Contracts.BlindTest;

/// <summary>
/// What a blind test round shows on the game master console: the title and the artist of the track from the start, for
/// the game master to judge the answers given out loud, then who has the hand.
/// </summary>
/// <param name="TrackNumber">The track in progress, from 1.</param>
/// <param name="TrackCount">How many tracks the round has.</param>
/// <param name="Phase">Phase of the track in progress.</param>
/// <param name="Title">The title of the track.</param>
/// <param name="Artist">The artist of the track, or <see langword="null"/> when the track is played on its title only.</param>
/// <param name="Winner">The nickname of the player who has the hand, or <see langword="null"/>.</param>
public sealed record BlindTestGameMasterView(
    int TrackNumber,
    int TrackCount,
    BlindTestTrackPhase Phase,
    string Title,
    string? Artist,
    string? Winner) : GameMasterRoundView;
