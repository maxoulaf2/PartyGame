using PartyGame.Contracts.Packs;

namespace PartyGame.Content;

/// <summary>
/// A media file referenced by the descriptor of a pack.
/// </summary>
/// <param name="Path">The JSON path of the reference in the descriptor.</param>
/// <param name="Media">The path of the media file, as written.</param>
internal sealed record MediaReference(string Path, MediaPath Media);
