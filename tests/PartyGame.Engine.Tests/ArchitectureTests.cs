using System.Diagnostics;
using System.Reflection;
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

    [Fact]
    public void EngineCode_Calls_ExcludeClockAndRandomness()
    {
        // Given
        var engine = typeof(GameEngine).Assembly;

        // When
        var forbidden = MethodCalls.In(engine)
            .Where(call => IsClockOrRandomness(call.Callee))
            .Select(call => $"{call.Caller.DeclaringType}.{call.Caller.Name} calls {call.Callee.DeclaringType}.{call.Callee.Name}")
            .Distinct()
            .Order(StringComparer.Ordinal)
            .ToList();

        // Then
        Assert.True(
            forbidden.Count == 0,
            $"{EngineProject} must take time and randomness from GameContext, but: {string.Join("; ", forbidden)}.");
    }

    [Fact]
    public void IsClockOrRandomness_CodeReadingClockOrCreatingRandom_IsDetected()
    {
        // When
        var detected = MethodCalls.InType(typeof(ImpureCode))
            .Where(call => IsClockOrRandomness(call.Callee))
            .Select(call => $"{call.Callee.DeclaringType!.Name}.{call.Callee.Name}")
            .ToHashSet();

        // Then
        Assert.Equal(["DateTime.get_Now", "DateTime.get_UtcNow", "Random..ctor", "Random.get_Shared"], detected.Order());
    }

    private static bool IsClockOrRandomness(MethodBase callee) => callee.DeclaringType switch
    {
        var t when t == typeof(DateTime) || t == typeof(DateTimeOffset) =>
            callee.Name is "get_Now" or "get_UtcNow" or "get_Today",
        var t when t == typeof(Random) => callee is ConstructorInfo || callee.Name == "get_Shared",
        var t when t == typeof(Guid) => callee.Name is "NewGuid" or "CreateVersion7",
        var t when t == typeof(Environment) => callee.Name is "get_TickCount" or "get_TickCount64",
        var t when t == typeof(TimeProvider) => callee.Name == "get_System",
        var t when t == typeof(Stopwatch) => true,
        _ => false,
    };

    private sealed class ImpureCode
    {
        public static object Run() => (DateTime.Now, DateTime.UtcNow, new Random(), Random.Shared);
    }
}
