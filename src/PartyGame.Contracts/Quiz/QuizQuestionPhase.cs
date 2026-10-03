namespace PartyGame.Contracts.Quiz;

/// <summary>
/// Phase of the question in progress in a quiz round, as the snapshots show it to the clients.
/// </summary>
/// <remarks>
/// The answers, their lock and the reveal come with US-E08-03 and US-E08-04.
/// </remarks>
public enum QuizQuestionPhase
{
    /// <summary>The question and its choices are shown, so that everybody reads them before the answers open.</summary>
    Presentation,
}
