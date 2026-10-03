namespace PartyGame.Contracts.Quiz;

/// <summary>
/// A player taking part in the question in progress, as the game master console lists them, with what they chose.
/// </summary>
/// <param name="PlayerId">Identifier of the player.</param>
/// <param name="Nickname">Nickname of the player, to show as plain text.</param>
/// <param name="Choice">The letter the player chose, or <see langword="null"/> while they have not answered.</param>
/// <param name="Points">
/// The points the player earned with the question, 0 included, once the answer is revealed; <see langword="null"/> before.
/// </param>
public sealed record QuizGameMasterAnswer(PlayerId PlayerId, string Nickname, QuizChoiceLetter? Choice, int? Points);
