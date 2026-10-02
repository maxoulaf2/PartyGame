using System.Collections.Immutable;

namespace PartyGame.Content;

/// <summary>
/// The packs found in the pack directory when the server started, valid or not.
/// </summary>
/// <param name="Directory">The full path of the pack directory.</param>
/// <param name="DirectoryExists">Whether the pack directory exists.</param>
/// <param name="Packs">The packs, in the ordinal order of their identifier.</param>
public sealed record PackLibrary(string Directory, bool DirectoryExists, ImmutableArray<LoadedPack> Packs);
