using PartyGame.Content;
using PartyGame.Engine.Packs;

namespace PartyGame.Server.Packs;

internal static class PackCatalogs
{
    /// <summary>
    /// What the engine keeps of the loaded packs: their content and their problems, without the paths on the disk nor the
    /// exceptions, which only the logs need.
    /// </summary>
    public static PackCatalog ToCatalog(this PackLibrary library)
    {
        ArgumentNullException.ThrowIfNull(library);
        return new PackCatalog(
            library.Directory,
            [.. library.Packs.Select(pack => new CatalogPack(pack.Id, pack.Title, pack.RoundCount, pack.Descriptor, pack.Problems)
            {
                Media = pack.Media,
            })]);
    }
}
