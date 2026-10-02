using System.Collections.Immutable;

namespace PartyGame.Contracts;

/// <summary>
/// A problem found in a pack when it is loaded, which makes the pack invalid. It carries no text meant for a human: the
/// client translates the code and inserts the parameters.
/// </summary>
/// <param name="Code">What is wrong.</param>
/// <param name="File">The file of the pack the problem is in, relative to its folder, such as <c>pack.json</c>.</param>
/// <param name="Path">
/// Where the problem is in the file, as a JSON path such as <c>$.rounds[1].questions[4].choices</c>, shown as is so that
/// the author finds it in the descriptor.
/// </param>
/// <param name="Parameters">The values the message of the code mentions, by name, as listed on each code.</param>
public sealed record PackProblem(PackProblemCode Code, string File, string Path, ImmutableDictionary<string, string> Parameters);
