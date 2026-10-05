namespace PartyGame.Contracts.Buzzer;

/// <summary>
/// A player buzzes on the question in progress.
/// </summary>
/// <param name="RoundId">The round the intent is aimed at.</param>
/// <param name="QuestionNumber">The question buzzed on, from 1.</param>
/// <param name="Opening">
/// The opening of the buzzer the player pressed during, as their view tells it: a buzz sent again after a reconnection
/// never counts for a later one.
/// </param>
/// <param name="PressedAt">
/// When the finger touched the screen, in server time as the phone estimates it, in milliseconds since the Unix epoch.
/// The server brings an implausible value back within bounds.
/// </param>
public sealed record BuzzerBuzz(RoundId RoundId, int QuestionNumber, int Opening, long PressedAt) : PlayerRoundIntent(RoundId);
