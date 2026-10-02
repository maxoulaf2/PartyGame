using System.Collections.Immutable;
using PartyGame.Contracts;
using PartyGame.Contracts.Packs;

namespace PartyGame.Content;

/// <summary>
/// Creates the problems found in the descriptor of a pack.
/// </summary>
internal static class Problems
{
    public static PackProblem InDescriptor(PackProblemCode code, string path, params (string Name, string Value)[] parameters) =>
        new(
            code,
            PackDescriptor.FileName,
            path,
            parameters.ToImmutableDictionary(parameter => parameter.Name, parameter => parameter.Value, StringComparer.Ordinal));
}
