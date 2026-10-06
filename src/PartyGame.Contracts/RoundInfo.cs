namespace PartyGame.Contracts;

/// <summary>
/// What every role knows of the round announced or in progress, or of the round that just finished between two rounds,
/// whatever its game mode.
/// </summary>
/// <param name="RoundId">Identifier of the round, which the game master names to go on to the next round.</param>
/// <param name="Number">Number of the round in the game, from 1.</param>
/// <param name="Count">Number of rounds of the game.</param>
/// <param name="Title">Title of the round, from the pack.</param>
/// <param name="Mode">
/// The game mode that plays the round, as the <c>type</c> of its activity in the pack, such as <c>quiz</c>: the clients
/// recall its rule when the round is announced.
/// </param>
/// <param name="Description">
/// What the author of the pack tells of the round, or <see langword="null"/> when the pack tells nothing.
/// </param>
public sealed record RoundInfo(RoundId RoundId, int Number, int Count, string Title, string Mode, string? Description);
