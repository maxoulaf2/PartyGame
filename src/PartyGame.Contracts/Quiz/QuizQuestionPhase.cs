namespace PartyGame.Contracts.Quiz;

/// <summary>
/// Phase of the question in progress in a quiz round, as the snapshots show it to the clients.
/// </summary>
public enum QuizQuestionPhase
{
    /// <summary>The question and its choices are shown, so that everybody reads them before the answers open.</summary>
    Presentation,

    /// <summary>The players answer, until every participant answered or the countdown ends.</summary>
    Answering,

    /// <summary>The answers are locked: no answer is accepted anymore.</summary>
    Locked,

    /// <summary>The correct answer is revealed, with what each player chose.</summary>
    Revealed,
}
