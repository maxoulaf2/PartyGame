using PartyGame.Contracts.Packs;

namespace PartyGame.Content;

/// <summary>
/// An activity of a pack that could be read, ready for the consistency check of its game mode.
/// </summary>
/// <param name="Path">The JSON path of the activity in the descriptor, such as <c>$.rounds[1]</c>.</param>
/// <param name="Round">The activity.</param>
internal sealed record RoundReference(string Path, RoundDescriptor Round);
