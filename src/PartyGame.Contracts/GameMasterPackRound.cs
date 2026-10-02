namespace PartyGame.Contracts;

/// <summary>
/// A round of a pack, as the game master sees it before choosing the pack: never its questions nor its answers.
/// </summary>
/// <param name="Title">The title of the round.</param>
/// <param name="Mode">The game mode that plays it, as the <c>type</c> of the activity names it, such as <c>quiz</c>.</param>
public sealed record GameMasterPackRound(string Title, string Mode);
