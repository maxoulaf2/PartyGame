using System.Collections.Immutable;

namespace PartyGame.Contracts;

/// <summary>
/// The packs the game master may choose from in the lobby, as the server last loaded them from its pack directory.
/// </summary>
/// <param name="Directory">The full path of the pack directory, so that the game master knows where the packs are read.</param>
/// <param name="Packs">Every pack found there, valid or not, in the ordinal order of their identifier.</param>
public sealed record GameMasterPackCatalog(string Directory, ImmutableArray<GameMasterPack> Packs);
