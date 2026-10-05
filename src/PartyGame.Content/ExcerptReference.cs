using PartyGame.Contracts.Packs;

namespace PartyGame.Content;

/// <summary>
/// An audio excerpt of a descriptor, whose start is checked against the duration of its track.
/// </summary>
/// <param name="Path">The JSON path of the excerpt in the descriptor.</param>
/// <param name="Excerpt">The excerpt.</param>
internal sealed record ExcerptReference(string Path, AudioExcerpt Excerpt);
