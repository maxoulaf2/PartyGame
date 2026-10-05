namespace PartyGame.Contracts.BlindTest;

/// <summary>
/// Phase of the track in progress in a blind test round, as the screens show it.
/// </summary>
public enum BlindTestTrackPhase
{
    /// <summary>
    /// The track is announced and its excerpt preloaded: the game master has yet to play it, the buzzer is closed.
    /// </summary>
    Ready,

    /// <summary>
    /// The excerpt plays, or played to its end: the buzzer is open, until the arbitration window designates a winner.
    /// </summary>
    Listening,

    /// <summary>A winner has the hand: the music is paused while they answer out loud.</summary>
    Answering,

    /// <summary>
    /// The title and the artist are revealed, once nothing is left to find, nobody is left to buzz, or the game master
    /// chose to: the buzzer is closed, the music stopped, and the elements found earned their points.
    /// </summary>
    Revealed,
}
