namespace PartyGame.Contracts.Quiz;

/// <summary>
/// The game master shows on the TV screen the next choice of the question presented, once they have read it aloud: the
/// choices show one by one, in the order of their letters, after the question.
/// </summary>
/// <param name="RoundId">The round the intent is aimed at.</param>
/// <param name="QuestionNumber">
/// The number of the question presented, from 1: the intent is obsolete once that question is no longer presented.
/// </param>
/// <param name="Choice">
/// The letter of the choice to show, which must be the next one: the intent is obsolete once that choice is shown, so
/// that a request sent twice, or by two consoles, never shows two choices.
/// </param>
public sealed record QuizShowChoice(RoundId RoundId, int QuestionNumber, QuizChoiceLetter Choice) : GameMasterRoundIntent(RoundId);
