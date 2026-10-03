namespace PartyGame.Contracts.Quiz;

/// <summary>
/// A choice of a quiz question, as the game master console shows it, the correct answer included.
/// </summary>
/// <param name="Letter">The letter of the choice, which is its position as shown on every screen.</param>
/// <param name="Text">The text of the choice.</param>
/// <param name="Correct">Whether this choice is the correct answer of the question.</param>
/// <param name="Shown">
/// Whether the TV screen shows it: the game master shows the choices one by one during the presentation, and opening
/// the answers shows them all.
/// </param>
/// <param name="AnswerCount">How many players chose it so far: 0 until the answers open.</param>
public sealed record QuizGameMasterChoice(QuizChoiceLetter Letter, string Text, bool Correct, bool Shown, int AnswerCount);
