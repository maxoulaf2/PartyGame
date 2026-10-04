using PartyGame.Tests.Shared;

namespace PartyGame.Bots.Tests;

public sealed class ArchitectureTests
{
    [Fact]
    public void BotsProject_Dependencies_IncludeOnlyContracts() =>
        ProjectDependencies.AssertProjectDependenciesAre("PartyGame.Bots", "PartyGame.Contracts");
}
