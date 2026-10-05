namespace PartyGame.Contracts.Buzzer;

/// <summary>
/// The game master asks the question announced: the buzzer opens on every phone, and the question shows on the TV screen
/// unless the game master reads it aloud first.
/// </summary>
/// <param name="RoundId">The round the intent is aimed at.</param>
/// <param name="QuestionNumber">The question asked, from 1: once asked, the intent sent again is obsolete.</param>
/// <param name="ShowQuestion">
/// Whether the question shows on the TV screen as the buzzer opens. When it does not, players may buzz while the game
/// master reads it aloud, and <see cref="BuzzerShowQuestion"/> shows it later.
/// </param>
public sealed record BuzzerAskQuestion(RoundId RoundId, int QuestionNumber, bool ShowQuestion) : GameMasterRoundIntent(RoundId);
