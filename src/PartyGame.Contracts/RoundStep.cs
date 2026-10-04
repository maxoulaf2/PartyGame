namespace PartyGame.Contracts;

/// <summary>
/// Where a round stands among its steps, such as the question in progress of a quiz, whatever its game mode.
/// </summary>
/// <param name="Number">Number of the step in progress, from 1.</param>
/// <param name="Count">Number of steps of the round.</param>
public sealed record RoundStep(int Number, int Count);
