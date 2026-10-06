using System.Collections.Immutable;
using PartyGame.Contracts;
using PartyGame.Contracts.Packs;

namespace PartyGame.Content;

/// <summary>
/// Creates the problems found in a pack.
/// </summary>
internal static class Problems
{
    public static PackProblem InDescriptor(PackProblemCode code, string path, params (string Name, string Value)[] parameters) =>
        new(
            code,
            PackDescriptor.FileName,
            path,
            parameters.ToImmutableDictionary(parameter => parameter.Name, parameter => parameter.Value, StringComparer.Ordinal));

    /// <summary>A problem of a whole file other than the descriptor, such as the zip of a pack.</summary>
    public static PackProblem InFile(PackProblemCode code, string file, params (string Name, string Value)[] parameters) =>
        new(
            code,
            file,
            JsonPath.Root,
            parameters.ToImmutableDictionary(parameter => parameter.Name, parameter => parameter.Value, StringComparer.Ordinal));
}
