namespace PartyGame.Contracts;

/// <summary>
/// The game master chooses the pack to play, in the lobby.
/// </summary>
/// <param name="PackId">
/// The <see cref="GameMasterPack.Id"/> of a valid pack of the <see cref="GameMasterSnapshot.PackCatalog"/>.
/// </param>
public sealed record SelectPackRequest(string PackId);
