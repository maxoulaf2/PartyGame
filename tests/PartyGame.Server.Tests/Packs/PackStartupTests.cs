using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using PartyGame.Content;
using PartyGame.Contracts.Packs;
using PartyGame.Server.Packs;
using PartyGame.Tests.Shared;

namespace PartyGame.Server.Tests.Packs;

public sealed class PackStartupTests : IDisposable
{
    private static readonly TimeSpan _startupTimeout = TimeSpan.FromSeconds(30);

    private const string ValidPack = """
        {
          "formatVersion": 1,
          "title": "Pack valide",
          "rounds": [
            { "type": "quiz", "title": "Manche", "questions": [{ "text": "Question ?", "choices": [{ "text": "Oui", "correct": true }, { "text": "Non" }] }] }
          ]
        }
        """;

    // Two problems: one found by reading the descriptor, one by the consistency check of the quiz mode.
    private const string InvalidPack = """
        {
          "formatVersion": 1,
          "title": "Pack invalide",
          "rounds": [
            { "type": "quiz", "title": "Manche", "answerSeconds": 1, "questions": [{ "text": "Question ?", "choices": [{ "text": "Oui" }, { "text": "Non" }] }] }
          ]
        }
        """;

    private readonly TempDirectory _logs = new();

    private readonly TempDirectory _packs = new();

    public void Dispose()
    {
        _logs.Dispose();
        _packs.Dispose();
    }

    [Fact]
    public async Task Startup_RepositoryPacks_AreLoadedValidWithTheRegisteredModes()
    {
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder
            .UseScratchDirectory(_logs.Path)
            .UseSetting(PacksOptions.DirectorySetting, Path.Combine(RepositoryRoot.Find(), "packs")));

        var library = factory.Services.GetRequiredService<PackLibrary>();

        Assert.True(library.DirectoryExists);
        var sample = Assert.Single(library.Packs, pack => pack.Id == "quiz-exemple");
        Assert.True(sample.IsValid);
        Assert.All(library.Packs, pack => Assert.Empty(pack.Problems));
    }

    [Fact]
    public async Task Startup_ValidAndInvalidPacks_StartsAndListsThemOnTheConsoleAndEachProblemInTheLog()
    {
        AddPack("a-valide", ValidPack);
        AddPack("b-invalide", InvalidPack);
        Directory.CreateDirectory(Path.Combine(_packs.Path, "c-pas-un-pack"));

        var port = ServerProcess.GetFreePort();
        using var server = ServerProcess.Start(ServerEnvironment(port, _packs.Path));
        using var client = new HttpClient();

        using var response = await server.WaitForResponseAsync(client, new Uri($"http://localhost:{port}/health"), _startupTimeout);
        var output = await server.WaitForOutputAsync("PartyGame est prêt", _startupTimeout);

        Assert.True(response.IsSuccessStatusCode);
        Assert.Contains($"Packs ({_packs.Path}) :", output, StringComparison.Ordinal);
        Assert.Contains("a-valide            : « Pack valide », 1 manche, valide", output, StringComparison.Ordinal);
        Assert.Contains("b-invalide          : « Pack invalide », 1 manche, invalide (2 problèmes)", output, StringComparison.Ordinal);
        Assert.DoesNotContain("c-pas-un-pack ", output, StringComparison.Ordinal);

        var entries = LogEntries();
        Assert.Contains(entries, entry => entry.Level == "Information" && entry.Properties.GetValueOrDefault("PackId") == "a-valide" && entry.Properties["ProblemCount"] == "0");
        Assert.Equal(
            [
                ("PackValueOutOfRange", "$.rounds[0].answerSeconds", "max=120, min=5"),
                ("QuizCorrectChoiceMissing", "$.rounds[0].questions[0]", string.Empty),
            ],
            entries.Where(entry => entry.Level == "Warning" && entry.Properties.GetValueOrDefault("PackId") == "b-invalide")
                .Select(entry => (entry.Properties["ProblemCode"], entry.Properties["Path"], entry.Properties["Parameters"])));
    }

    [Fact]
    public async Task Startup_MissingPackDirectory_StartsAndSaysSo()
    {
        var missing = Path.Combine(_packs.Path, "absent");
        var port = ServerProcess.GetFreePort();
        using var server = ServerProcess.Start(ServerEnvironment(port, missing));
        using var client = new HttpClient();

        using var response = await server.WaitForResponseAsync(client, new Uri($"http://localhost:{port}/health"), _startupTimeout);
        var output = await server.WaitForOutputAsync("PartyGame est prêt", _startupTimeout);

        Assert.True(response.IsSuccessStatusCode);
        Assert.Contains($"Aucun pack : le dossier {missing} est introuvable.", output, StringComparison.Ordinal);
        Assert.Contains(LogEntries(), entry => entry.Level == "Warning" && entry.Properties.GetValueOrDefault("Directory") == missing);
    }

    [Fact]
    public void FullDirectory_RelativeDirectory_IsRelativeToTheApplicationFolder()
    {
        var options = new PacksOptions { Directory = Path.Combine("contenu", "packs") };

        Assert.Equal(Path.Combine(AppContext.BaseDirectory, "contenu", "packs"), options.FullDirectory);
        Assert.Equal(Path.Combine(AppContext.BaseDirectory, "packs"), new PacksOptions().FullDirectory);
    }

    [Fact]
    public void FullDirectory_AbsoluteDirectory_IsKept()
    {
        var options = new PacksOptions { Directory = _packs.Path };

        Assert.Equal(_packs.Path, options.FullDirectory);
    }

    // The log files hold one compact JSON object per event, without "@l" for the Information level.
    private List<(string Level, Dictionary<string, string> Properties)> LogEntries() =>
        [.. _logs.ReadAllLogs()
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(line =>
            {
                using var entry = JsonDocument.Parse(line);
                var properties = entry.RootElement.EnumerateObject()
                    .Where(property => property.Value.ValueKind is JsonValueKind.String or JsonValueKind.Number)
                    .ToDictionary(property => property.Name, property => property.Value.ToString(), StringComparer.Ordinal);
                return (properties.GetValueOrDefault("@l") ?? "Information", properties);
            })];

    private void AddPack(string id, string descriptor)
    {
        var folder = Path.Combine(_packs.Path, id);
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, PackDescriptor.FileName), descriptor);
    }

    private Dictionary<string, string> ServerEnvironment(int port, string packDirectory) => new()
    {
        ["Network__Port"] = port.ToString(CultureInfo.InvariantCulture),
        ["LogFiles__Directory"] = _logs.Path,
        ["Persistence__Directory"] = _logs.Path,
        ["Packs__Directory"] = packDirectory,
    };
}
