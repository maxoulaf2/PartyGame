namespace PartyGame.Contracts;

/// <summary>
/// The answer of the hub to a <see cref="RenamePlayerRequest"/>. The new nickname itself reaches every client through
/// the snapshots.
/// </summary>
/// <param name="Refusal">Why the rename was refused, or <see langword="null"/> when the player has the new nickname.</param>
public sealed record RenamePlayerResult(RenamePlayerRefusal? Refusal);
