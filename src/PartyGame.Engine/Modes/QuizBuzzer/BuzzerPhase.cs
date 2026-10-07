namespace PartyGame.Engine.Modes.QuizBuzzer;

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

    /// <summary>
    /// Every connected player is blocked after a wrong answer: the buzzer stays closed until the game master reveals the
    /// answer.
    /// </summary>
    Closed,

    /// <summary>The expected answer is revealed, and the points of the question awarded.</summary>
    Revealed,
}
