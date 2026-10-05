namespace PartyGame.Contracts.OpenQuestion;

/// <summary>
/// The game master gives up the question in progress: it scores nothing, its answers are ignored, and the next question of
/// the round is presented, or the round ends after its last one.
/// </summary>
/// <param name="RoundId">The round the intent is aimed at.</param>
/// <param name="QuestionNumber">
/// The number of the question to skip, from 1: the intent is obsolete once the round has moved on, so that a request
/// sent twice, or by two consoles, never skips the next question too.
/// </param>
public sealed record OpenQuestionSkipQuestion(RoundId RoundId, int QuestionNumber) : GameMasterRoundIntent(RoundId);
