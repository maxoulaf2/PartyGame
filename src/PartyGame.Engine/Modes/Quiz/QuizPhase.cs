namespace PartyGame.Engine.Modes.Quiz;

/// <summary>
/// Phase of the question in progress in a quiz round.
/// </summary>
public enum QuizPhase
{
    /// <summary>
    /// The game master reads out the question, then its choices one by one, each shown on the TV screen as they go. The
    /// answers open with the first choice: the players may choose among the choices shown.
    /// </summary>
    Presentation,

    /// <summary>
    /// Every choice shown, the countdown runs: the players answer, until every participant answered or the countdown ends.
    /// </summary>
    Answering,

    /// <summary>The answers are locked: no answer is accepted anymore.</summary>
    Locked,

    /// <summary>The correct answer is revealed, with what each player chose.</summary>
    Revealed,
}
