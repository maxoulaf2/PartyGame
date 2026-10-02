using PartyGame.Contracts.Packs;
using PartyGame.Tests.Shared;

namespace PartyGame.TypeGen.Tests;

/// <summary>
/// The TypeScript types committed in the client must match PartyGame.Contracts. This test is the reference
/// check: the .NET SDK is always there when it runs, unlike in <c>npm run check</c>.
/// </summary>
public sealed class GeneratedContractsTests
{
    [Fact]
    public void GeneratedFiles_CommittedInClient_MatchContractsAssembly()
    {
        var directory = Path.Combine(RepositoryRoot.Find(), "client", "src", "shared", "contracts");

        var differences = OutputDirectory.FindDifferences(directory, ContractsGenerator.Generate(ContractsGenerator.ContractsAssembly));

        Assert.True(
            differences.Count == 0,
            $"Generated TypeScript contracts are out of date: {string.Join("; ", differences)}. " +
            "Run `npm run generate:contracts` from client/ to update them.");
    }

    [Fact]
    public void Generate_ContractsAssembly_Succeeds()
    {
        var files = ContractsGenerator.Generate(ContractsGenerator.ContractsAssembly);

        Assert.Contains(files, f => f.Path == "PlayerId.ts");
        Assert.Contains(files, f => f.Path == "Role.ts");
        Assert.Contains(files, f => f.Path == "IGameClient.ts");
        Assert.DoesNotContain(files, f => f.Path.StartsWith("ContractJsonOptions", StringComparison.Ordinal));
    }

    [Fact]
    public void Generate_ContractsAssembly_ExcludesPackDescriptors()
    {
        var packTypes = typeof(PackDescriptor).Assembly.GetExportedTypes()
            .Where(t => t.Namespace == typeof(PackDescriptor).Namespace)
            .Select(t => t.Name)
            .ToList();

        var files = ContractsGenerator.Generate(ContractsGenerator.ContractsAssembly);

        Assert.Contains(nameof(PackDescriptor), packTypes);
        Assert.Contains(nameof(RoundDescriptor), packTypes);
        Assert.All(files, file => Assert.DoesNotContain(Path.GetFileNameWithoutExtension(file.Path), packTypes));
        Assert.All(files, file => Assert.DoesNotContain(nameof(PackDescriptor), file.Content, StringComparison.Ordinal));
    }
}
