using PartyGame.Tests.Shared;

namespace PartyGame.Contracts.Tests;

public sealed class ArchitectureTests
{
    [Fact]
    public void ContractsProject_Dependencies_IncludeNoOtherProject() =>
        ProjectDependencies.AssertProjectDependenciesAre("PartyGame.Contracts");
}
