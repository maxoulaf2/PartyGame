namespace PartyGame.Contracts.Quiz;

/// <summary>
/// A choice of a quiz question, as the TV screen and the phones show it: never whether it is the correct one before the
/// reveal.
/// </summary>
/// <param name="Letter">The letter of the choice, which is its position as shown.</param>
/// <param name="Text">The text of the choice.</param>
public sealed record QuizChoiceView(QuizChoiceLetter Letter, string Text);
