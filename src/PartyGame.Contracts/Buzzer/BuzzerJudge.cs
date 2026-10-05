namespace PartyGame.Contracts.Buzzer;

/// <summary>
/// The game master judges the answer the player who has the hand gave out loud: a correct one wins the points of the round
/// and reveals the answer, a wrong one blocks the player for the question and opens the buzzer anew to the others.
/// </summary>
/// <param name="RoundId">The round the intent is aimed at.</param>
/// <param name="QuestionNumber">The question judged, from 1.</param>
/// <param name="Opening">
/// The opening of the buzzer the player won, as the console tells it: a judgment sent twice, or by two consoles, is
/// obsolete once the buzzer opens anew, and never judges the next winner.
/// </param>
/// <param name="Correct">Whether the answer is correct.</param>
public sealed record BuzzerJudge(RoundId RoundId, int QuestionNumber, int Opening, bool Correct) : GameMasterRoundIntent(RoundId);
