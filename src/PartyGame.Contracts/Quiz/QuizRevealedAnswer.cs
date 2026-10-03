namespace PartyGame.Contracts.Quiz;

/// <summary>
/// A player who took part in the question revealed, as the TV screen shows them, with what they chose.
/// </summary>
/// <param name="PlayerId">Identifier of the player.</param>
/// <param name="Nickname">Nickname of the player, to show as plain text.</param>
/// <param name="Choice">The letter the player chose, or <see langword="null"/> when they did not answer.</param>
public sealed record QuizRevealedAnswer(PlayerId PlayerId, string Nickname, QuizChoiceLetter? Choice);
