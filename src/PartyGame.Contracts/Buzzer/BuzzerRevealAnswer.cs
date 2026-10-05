namespace PartyGame.Contracts.Buzzer;

/// <summary>
/// The game master reveals the expected answer of the question asked, without points: nobody found it, or nobody may
/// buzz anymore. The buzzer closes.
/// </summary>
/// <param name="RoundId">The round the intent is aimed at.</param>
/// <param name="QuestionNumber">The question to reveal, from 1: once revealed, the intent sent again is obsolete.</param>
public sealed record BuzzerRevealAnswer(RoundId RoundId, int QuestionNumber) : GameMasterRoundIntent(RoundId);
