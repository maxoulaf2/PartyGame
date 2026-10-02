namespace PartyGame.Contracts;

/// <summary>
/// The answer of the hub when the game master reloads the packs. The new catalog itself reaches every console through the
/// snapshots.
/// </summary>
/// <param name="Refusal">Why the reload was refused, or <see langword="null"/> when the packs are reloaded.</param>
public sealed record ReloadPacksResult(ReloadPacksRefusal? Refusal);
