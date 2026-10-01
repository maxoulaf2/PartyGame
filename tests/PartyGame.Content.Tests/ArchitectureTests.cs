using PartyGame.Tests.Shared;

namespace PartyGame.Content.Tests;

public sealed class ArchitectureTests
{
    [Fact]
    public void ContentProject_Dependencies_IncludeOnlyContracts() =>
        ProjectDependencies.AssertProjectDependenciesAre("PartyGame.Content", "PartyGame.Contracts");
}
