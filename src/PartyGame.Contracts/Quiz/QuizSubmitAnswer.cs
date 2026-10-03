namespace PartyGame.Contracts.Quiz;

/// <summary>
/// A player chooses a choice of the question in progress, among those the TV screen shows, while its answers are open:
/// from the first choice shown until they are locked. Only the first choice of a player counts: a second one is rejected.
/// </summary>
/// <param name="RoundId">The round the intent is aimed at.</param>
/// <param name="QuestionNumber">
/// The number of the question the player answers, from 1: an answer delayed by the network never counts for the next
/// question.
/// </param>
/// <param name="Choice">The letter of the chosen choice, as shown on every screen.</param>
public sealed record QuizSubmitAnswer(RoundId RoundId, int QuestionNumber, QuizChoiceLetter Choice) : PlayerRoundIntent(RoundId);
