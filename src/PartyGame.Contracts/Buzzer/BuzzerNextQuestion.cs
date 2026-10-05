namespace PartyGame.Contracts.Buzzer;

/// <summary>
/// The game master moves on from the revealed question: the next question of the round is announced, or the round ends
/// after its last one.
/// </summary>
/// <param name="RoundId">The round the intent is aimed at.</param>
/// <param name="QuestionNumber">
/// The number of the revealed question to move on from, from 1: the intent is obsolete once the round has moved on, so
/// that a request sent twice, or by two consoles, never skips a question.
/// </param>
public sealed record BuzzerNextQuestion(RoundId RoundId, int QuestionNumber) : GameMasterRoundIntent(RoundId);
