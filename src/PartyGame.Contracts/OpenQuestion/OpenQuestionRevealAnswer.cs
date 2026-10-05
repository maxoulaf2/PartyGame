namespace PartyGame.Contracts.OpenQuestion;

/// <summary>
/// The game master reveals the expected answer of the question judged, and what every player answered: the points of the
/// question are awarded now.
/// </summary>
/// <param name="RoundId">The round the intent is aimed at.</param>
/// <param name="QuestionNumber">
/// The number of the question to reveal, from 1: the intent is obsolete once it is revealed or no longer in progress, so
/// that a reveal sent twice, or by two consoles, counts the points once.
/// </param>
public sealed record OpenQuestionRevealAnswer(RoundId RoundId, int QuestionNumber) : GameMasterRoundIntent(RoundId);
