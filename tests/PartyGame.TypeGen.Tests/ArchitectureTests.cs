using PartyGame.Tests.Shared;

namespace PartyGame.TypeGen.Tests;

public sealed class ArchitectureTests
{
    [Fact]
    public void TypeGenProject_Dependencies_IncludeOnlyContracts() =>
        ProjectDependencies.AssertProjectDependenciesAre("PartyGame.TypeGen", "PartyGame.Contracts");
}
