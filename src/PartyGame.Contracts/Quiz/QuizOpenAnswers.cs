namespace PartyGame.Contracts.Quiz;

/// <summary>
/// The game master opens the answers of the question presented: the countdown starts on every screen.
/// </summary>
/// <param name="RoundId">The round the intent is aimed at.</param>
/// <param name="QuestionNumber">
/// The number of the question to open, from 1: the intent is obsolete once that question is no longer presented.
/// </param>
public sealed record QuizOpenAnswers(RoundId RoundId, int QuestionNumber) : GameMasterRoundIntent(RoundId);
