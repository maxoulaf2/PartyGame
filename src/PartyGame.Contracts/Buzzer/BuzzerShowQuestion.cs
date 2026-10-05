namespace PartyGame.Contracts.Buzzer;

/// <summary>
/// The game master shows on the TV screen the question asked with its buzzer open but the question hidden, once they have
/// read it aloud.
/// </summary>
/// <param name="RoundId">The round the intent is aimed at.</param>
/// <param name="QuestionNumber">The question to show, from 1: once shown, the intent sent again is obsolete.</param>
public sealed record BuzzerShowQuestion(RoundId RoundId, int QuestionNumber) : GameMasterRoundIntent(RoundId);
