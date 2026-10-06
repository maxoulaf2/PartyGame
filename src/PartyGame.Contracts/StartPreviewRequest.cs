namespace PartyGame.Contracts;

/// <summary>
/// The game master previews a pack on the TV screen, in the lobby: every step of its rounds, answer included, from the first
/// one.
/// </summary>
/// <param name="PackId">
/// The <see cref="GameMasterPack.Id"/> of a valid pack of the <see cref="GameMasterSnapshot.PackCatalog"/>.
/// </param>
public sealed record StartPreviewRequest(string PackId);
