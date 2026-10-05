namespace PartyGame.Engine.Modes.BlindTest;

/// <summary>
/// Phase of the track in progress in a blind test round, derived from its buzzer.
/// </summary>
public enum BlindTestPhase
{
    /// <summary>The track is announced: the game master has yet to play its excerpt, the buzzer is closed.</summary>
    Ready,

    /// <summary>The excerpt plays, or played to its end: the buzzer is open, and nobody buzzed yet.</summary>
    Listening,

    /// <summary>
    /// The first buzz arrived: the arbitration window runs, the buzzer stays open and the music plays on.
    /// </summary>
    Arbitrating,

    /// <summary>The arbitration designated a winner, who has the hand: the music is paused.</summary>
    Answering,
}
