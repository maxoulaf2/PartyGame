namespace PartyGame.Contracts.Quiz;

/// <summary>
/// The game master locks the answers before the end of the countdown, for instance once everybody answered.
/// </summary>
/// <param name="RoundId">The round the intent is aimed at.</param>
/// <param name="QuestionNumber">
/// The number of the question to lock, from 1: the intent is obsolete once its answers are no longer open.
/// </param>
public sealed record QuizLockAnswers(RoundId RoundId, int QuestionNumber) : GameMasterRoundIntent(RoundId);
