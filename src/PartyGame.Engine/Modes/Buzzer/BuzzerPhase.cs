namespace PartyGame.Engine.Modes.Buzzer;

/// <summary>
/// Phase of the question in progress in a round of buzzer questions, derived from its buzzer.
/// </summary>
public enum BuzzerPhase
{
    /// <summary>The question is announced: the game master has yet to ask it, the buzzer is closed.</summary>
    Ready,

    /// <summary>The question is asked: the buzzer is open, and nobody buzzed yet.</summary>
    Open,

    /// <summary>
    /// The first buzz arrived: the arbitration window runs, and the buzzer stays open, so that a player who pressed earlier
    /// but whose buzz arrives later still wins.
    /// </summary>
    Arbitrating,

    /// <summary>The arbitration designated a winner, who has the hand.</summary>
    Answering,
}
