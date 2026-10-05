namespace PartyGame.Contracts.Buzzer;

/// <summary>
/// The game master asks the question announced: it shows on the TV screen, and the buzzer opens on every phone.
/// </summary>
/// <param name="RoundId">The round the intent is aimed at.</param>
/// <param name="QuestionNumber">The question asked, from 1: once asked, the intent sent again is obsolete.</param>
public sealed record BuzzerAskQuestion(RoundId RoundId, int QuestionNumber) : GameMasterRoundIntent(RoundId);
