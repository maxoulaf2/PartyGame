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
        var directory = Path.Combine(FindRepositoryRoot(), "client", "src", "shared", "contracts");

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

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "PartyGame.slnx")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException($"PartyGame.slnx not found above {AppContext.BaseDirectory}.");
    }
}
