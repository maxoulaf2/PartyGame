namespace PartyGame.Contracts.Quiz;

/// <summary>
/// The game master shows on the TV screen the question presented, with its image, once they have read it aloud: its
/// choices stay hidden until the game master shows them one by one.
/// </summary>
/// <param name="RoundId">The round the intent is aimed at.</param>
/// <param name="QuestionNumber">
/// The number of the question to show, from 1: the intent is obsolete once that question is shown or no longer presented,
/// so that a request sent twice, or by two consoles, changes nothing.
/// </param>
public sealed record QuizShowQuestion(RoundId RoundId, int QuestionNumber) : GameMasterRoundIntent(RoundId);
