namespace PartyGame.Contracts.Buzzer;

/// <summary>
/// Phase of the question in progress in a round of buzzer questions, as the screens show it.
/// </summary>
public enum BuzzerQuestionPhase
{
    /// <summary>The question is announced: the game master has yet to ask it, the buzzer is closed.</summary>
    Ready,

    /// <summary>
    /// The question is asked and shows on the TV screen: the buzzer is open, until the arbitration window designates a
    /// winner.
    /// </summary>
    Open,

    /// <summary>A winner has the hand: they answer out loud, the buzzer is closed.</summary>
    Answering,

    /// <summary>
    /// Every connected player is blocked after a wrong answer: the buzzer stays closed, and the game master may only reveal
    /// the answer.
    /// </summary>
    Closed,

    /// <summary>The expected answer is revealed, with who found it, if anybody.</summary>
    Revealed,
}
