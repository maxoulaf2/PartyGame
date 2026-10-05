namespace PartyGame.Contracts.OpenQuestion;

/// <summary>
/// A player taking part in the question in progress, as the game master console lists them, with what they answered.
/// </summary>
/// <param name="PlayerId">Identifier of the player.</param>
/// <param name="Nickname">Nickname of the player, to show as plain text.</param>
/// <param name="Answer">The answer of the player, as typed, or <see langword="null"/> while they have not answered.</param>
/// <param name="Points">
/// The points the player earned with the question, 0 included, once revealed; <see langword="null"/> before.
/// </param>
public sealed record OpenQuestionGameMasterAnswer(PlayerId PlayerId, string Nickname, string? Answer, int? Points);
