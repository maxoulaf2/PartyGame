namespace PartyGame.Contracts;

/// <summary>
/// The answer of the hub to a <see cref="SelectPackRequest"/>. The selection itself reaches every console and the TV
/// screen through the snapshots.
/// </summary>
/// <param name="Refusal">Why the choice was refused, or <see langword="null"/> when the pack is selected.</param>
public sealed record SelectPackResult(SelectPackRefusal? Refusal);
