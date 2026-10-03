namespace PartyGame.Engine.Modes.Quiz;

/// <summary>
/// Phase of the question in progress in a quiz round.
/// </summary>
/// <remarks>
/// The reveal comes with US-E08-04.
/// </remarks>
public enum QuizPhase
{
    /// <summary>The question and its choices are shown, so that everybody reads them before the answers open.</summary>
    Presentation,

    /// <summary>The players answer, until the countdown ends or the game master locks the answers.</summary>
    Answering,

    /// <summary>The answers are locked: no answer is accepted anymore.</summary>
    Locked,
}
