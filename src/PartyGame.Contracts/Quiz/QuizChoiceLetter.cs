namespace PartyGame.Contracts.Quiz;

/// <summary>
/// The letter of a choice of a quiz question: its position as shown, the same on every screen. Each letter goes with a
/// shape and a color, so that a choice is never told apart by its color alone.
/// </summary>
/// <remarks>
/// The projections and the intents name a choice by its letter, never by its position in the descriptor: the order of
/// the descriptor, often with the correct answer first, would show through the shuffle.
/// </remarks>
public enum QuizChoiceLetter
{
    /// <summary>The first choice shown.</summary>
    A,

    /// <summary>The second choice shown.</summary>
    B,

    /// <summary>The third choice shown.</summary>
    C,

    /// <summary>The fourth choice shown.</summary>
    D,
}
