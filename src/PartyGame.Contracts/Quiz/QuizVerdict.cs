namespace PartyGame.Contracts.Quiz;

/// <summary>
/// What the reveal of a question tells a player who took part in it.
/// </summary>
public enum QuizVerdict
{
    /// <summary>The player chose the correct answer.</summary>
    Correct,

    /// <summary>The player chose another choice.</summary>
    Wrong,

    /// <summary>The player took part but did not answer before the answers were locked.</summary>
    NoAnswer,
}
