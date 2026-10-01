using System.Collections.Immutable;
using System.Reflection;
using PartyGame.Contracts;
using PartyGame.Contracts.Serialization;

namespace PartyGame.TypeGen;

/// <summary>
/// Entry point of the generation, free of any I/O: types in, files out.
/// </summary>
internal static class ContractsGenerator
{
    /// <summary>
    /// The assembly whose types are exposed to the clients.
    /// </summary>
    public static Assembly ContractsAssembly => typeof(Role).Assembly;

    /// <summary>
    /// Generates the TypeScript files of every public type of the contracts assembly,
    /// except the serialization infrastructure.
    /// </summary>
    public static ImmutableArray<GeneratedFile> Generate(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        var infrastructure = typeof(ContractJsonOptions).Namespace;
        var roots = assembly.GetExportedTypes()
            .Where(t => t.Namespace != infrastructure)
            .OrderBy(t => t.FullName, StringComparer.Ordinal);

        return Generate(roots);
    }

    /// <summary>
    /// Generates the TypeScript files of the given types and of every type they reference.
    /// </summary>
    public static ImmutableArray<GeneratedFile> Generate(IEnumerable<Type> roots) =>
        TypeScriptWriter.Write(TypeModelBuilder.Build(roots));
}
