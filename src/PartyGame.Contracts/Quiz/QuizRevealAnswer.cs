namespace PartyGame.Contracts.Quiz;

/// <summary>
/// The game master reveals the correct answer of the question whose answers are locked, and what each player chose.
/// </summary>
/// <param name="RoundId">The round the intent is aimed at.</param>
/// <param name="QuestionNumber">
/// The number of the question to reveal, from 1: the intent is obsolete once its answers are no longer locked.
/// </param>
public sealed record QuizRevealAnswer(RoundId RoundId, int QuestionNumber) : GameMasterRoundIntent(RoundId);
