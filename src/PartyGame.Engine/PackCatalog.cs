using System.Collections.Immutable;

namespace PartyGame.Engine;

/// <summary>
/// The packs of the pack directory, as the server last loaded and checked them: the game master chooses the pack of the
/// game among the valid ones. The engine never reads a file: the server loads the packs, then hands their result over.
/// </summary>
/// <param name="Directory">The full path of the pack directory, shown to the game master.</param>
/// <param name="Packs">Every pack found there, valid or not, in the ordinal order of their identifier.</param>
public sealed record PackCatalog(string Directory, ImmutableArray<CatalogPack> Packs)
{
    /// <summary>
    /// Finds a pack by its identifier.
    /// </summary>
    /// <param name="id">The identifier of the pack: the name of its folder, compared as is.</param>
    /// <returns>The pack, or <see langword="null"/> when the catalog has none with this identifier.</returns>
    public CatalogPack? Find(string id) =>
        Packs.FirstOrDefault(pack => string.Equals(pack.Id, id, StringComparison.Ordinal));
}
