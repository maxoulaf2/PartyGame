using System.Collections.Immutable;
using PartyGame.Contracts;
using PartyGame.Contracts.Packs;

namespace PartyGame.Content;

/// <summary>
/// Checks the consistency of an activity of a pack, as its game mode understands it. The server provides it from the
/// registered modes: the loading of the packs does not know them.
/// </summary>
/// <param name="round">An activity that could be read, whose simple constraints were checked.</param>
/// <param name="path">The JSON path of the activity in the descriptor, such as <c>$.rounds[1]</c>.</param>
/// <returns>The problems found, located under <paramref name="path"/>, or none.</returns>
public delegate ImmutableArray<PackProblem> RoundValidator(RoundDescriptor round, string path);
