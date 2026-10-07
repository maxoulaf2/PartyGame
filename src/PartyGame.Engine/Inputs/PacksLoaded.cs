using PartyGame.Engine.Packs;

namespace PartyGame.Engine.Inputs;

/// <summary>
/// The server loaded and checked the packs again, at the request of the game master: the files are read outside the loop,
/// and only their result reaches the engine.
/// </summary>
/// <param name="Catalog">The packs as they are now on the disk.</param>
public sealed record PacksLoaded(PackCatalog Catalog) : GameInput;
