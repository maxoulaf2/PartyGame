using System.Reflection;
using System.Text.Json;

namespace PartyGame.Tests.Shared;

/// <summary>
/// Reads the dependencies of a project from two sources: the project and package references declared
/// at build time (from the test host deps.json, which catches references not used in code yet),
/// and the assemblies actually referenced in the compiled metadata.
/// </summary>
internal static class ProjectDependencies
{
    public static IReadOnlySet<string> Declared(string projectName)
    {
        var testAssemblyName = typeof(ProjectDependencies).Assembly.GetName().Name;
        var depsPath = Path.Combine(AppContext.BaseDirectory, $"{testAssemblyName}.deps.json");
        using var deps = JsonDocument.Parse(File.ReadAllText(depsPath));

        foreach (var target in deps.RootElement.GetProperty("targets").EnumerateObject())
        {
            foreach (var library in target.Value.EnumerateObject())
            {
                if (library.Name.StartsWith($"{projectName}/", StringComparison.Ordinal))
                {
                    return library.Value.TryGetProperty("dependencies", out var dependencies)
                        ? dependencies.EnumerateObject().Select(d => d.Name).ToHashSet(StringComparer.Ordinal)
                        : [];
                }
            }
        }

        throw new InvalidOperationException($"Project {projectName} not found in {depsPath}.");
    }

    public static IReadOnlySet<string> Referenced(string projectName) =>
        Assembly.Load(new AssemblyName(projectName))
            .GetReferencedAssemblies()
            .Select(a => a.Name!)
            .ToHashSet(StringComparer.Ordinal);

    public static IReadOnlySet<string> All(string projectName) =>
        Declared(projectName).Union(Referenced(projectName)).ToHashSet(StringComparer.Ordinal);

    public static void AssertProjectDependenciesAre(string projectName, params string[] allowedProjects)
    {
        var forbidden = All(projectName)
            .Where(name => name.StartsWith("PartyGame.", StringComparison.Ordinal) && !allowedProjects.Contains(name))
            .Order(StringComparer.Ordinal)
            .ToList();

        var allowed = allowedProjects.Length == 0 ? "no other project" : string.Join(", ", allowedProjects);
        Assert.True(
            forbidden.Count == 0,
            $"{projectName} must depend on {allowed}, but references {string.Join(", ", forbidden)}.");
    }
}
