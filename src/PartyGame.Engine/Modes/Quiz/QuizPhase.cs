namespace PartyGame.Engine.Modes.Quiz;

/// <summary>
/// Phase of the question in progress in a quiz round.
/// </summary>
/// <remarks>
/// The answers, their lock and the reveal come with US-E08-03 and US-E08-04.
/// </remarks>
public enum QuizPhase
{
    /// <summary>The question and its choices are shown, so that everybody reads them before the answers open.</summary>
    Presentation,
}
