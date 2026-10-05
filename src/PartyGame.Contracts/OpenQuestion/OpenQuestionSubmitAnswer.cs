namespace PartyGame.Contracts.OpenQuestion;

/// <summary>
/// A player sends their answer to the question in progress while its answers are open. Only the first answer of a player
/// counts: a second one is rejected.
/// </summary>
/// <param name="RoundId">The round the intent is aimed at.</param>
/// <param name="QuestionNumber">
/// The number of the question the player answers, from 1: an answer delayed by the network never counts for the next
/// question.
/// </param>
/// <param name="Answer">
/// The answer as typed, never truncated: rejected when longer than the <c>maxLength</c> of the round, or empty once
/// normalized.
/// </param>
public sealed record OpenQuestionSubmitAnswer(RoundId RoundId, int QuestionNumber, string Answer) : PlayerRoundIntent(RoundId);
