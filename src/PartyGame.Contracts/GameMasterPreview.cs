namespace PartyGame.Contracts;

/// <summary>
/// The step of a pack the TV screen previews, as the game master console drives it.
/// </summary>
/// <param name="PackId">The pack previewed, among those of the <see cref="GameMasterSnapshot.PackCatalog"/>.</param>
/// <param name="Round">The round of the pack the step belongs to.</param>
/// <param name="Step">Where the step stands among those of its round.</param>
/// <param name="HasExcerpt">Whether the step has an excerpt the TV screen can play.</param>
public sealed record GameMasterPreview(string PackId, RoundInfo Round, RoundStep Step, bool HasExcerpt);
