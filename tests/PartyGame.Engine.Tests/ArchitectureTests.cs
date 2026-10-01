using PartyGame.Tests.Shared;

namespace PartyGame.Engine.Tests;

public sealed class ArchitectureTests
{
    private const string EngineProject = "PartyGame.Engine";

    [Fact]
    public void EngineProject_Dependencies_IncludeOnlyContracts() =>
        ProjectDependencies.AssertProjectDependenciesAre(EngineProject, "PartyGame.Contracts");

    [Fact]
    public void EngineProject_Dependencies_ExcludeAspNetCore()
    {
        // The engine stays pure. SignalR assemblies are named Microsoft.AspNetCore.SignalR*, so this prefix covers them too.
        var aspNetCore = ProjectDependencies.All(EngineProject)
            .Where(name => name.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .ToList();

        Assert.True(
            aspNetCore.Count == 0,
            $"{EngineProject} must not reference ASP.NET Core or SignalR, but references {string.Join(", ", aspNetCore)}.");
    }
}
