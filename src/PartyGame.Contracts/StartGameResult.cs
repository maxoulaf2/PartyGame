namespace PartyGame.Contracts;

/// <summary>
/// The answer of the hub when the game master starts the game. The new phase itself reaches every client through the
/// snapshots.
/// </summary>
/// <param name="Refusal">Why the start was refused, or <see langword="null"/> when the game is started.</param>
public sealed record StartGameResult(StartGameRefusal? Refusal);
