using System.Text.Json.Nodes;
using PartyGame.Contracts.Packs;
using PartyGame.Tests.Shared;

namespace PartyGame.TypeGen.Tests;

/// <summary>
/// The pack schema committed in <c>schemas/</c> must match PartyGame.Contracts.Packs, like the TypeScript contracts.
/// </summary>
public sealed class GeneratedPackSchemaTests
{
    [Fact]
    public void GeneratedSchema_CommittedInRepository_MatchesPackDescriptors()
    {
        var path = Path.Combine(RepositoryRoot.Find(), "schemas", "pack.schema.json");

        var difference = OutputFile.FindDifference(path, PackSchemaGenerator.Generate(), "schemas/pack.schema.json");

        Assert.True(
            difference is null,
            $"The pack schema is out of date: {difference}. Run `npm run generate:contracts` from client/ to update it.");
    }

    [Fact]
    public void Generate_PackDescriptor_OffersEveryActivityTypeAndTheFormatVersion()
    {
        var schema = JsonNode.Parse(PackSchemaGenerator.Generate())!;

        var types = schema["properties"]!["rounds"]!["items"]!["anyOf"]!.AsArray()
            .Select(variant => (string?)variant!["properties"]!["type"]!["const"]);

        Assert.Equal(["quiz", "buzzer", "blindtest", "openquestion"], types);
        Assert.Equal(PackDescriptor.CurrentFormatVersion, (int?)schema["properties"]!["formatVersion"]!["const"]);
    }
}
