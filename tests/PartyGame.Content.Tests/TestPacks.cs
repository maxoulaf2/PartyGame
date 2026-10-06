using System.Collections.Immutable;
using PartyGame.Contracts;
using PartyGame.Contracts.Packs;

namespace PartyGame.Content.Tests;

/// <summary>
/// A pack directory of its own for each test, deleted afterwards, where each test writes the packs it needs.
/// </summary>
internal sealed class TestPacks : IDisposable
{
    public const string ValidQuestion = """{ "text": "Question ?", "choices": [{ "text": "Oui", "correct": true }, { "text": "Non" }] }""";

    public string Root { get; } = Path.Combine(Path.GetTempPath(), "partygame-tests", Guid.NewGuid().ToString("N"));

    /// <summary>The folder the zip packs of <see cref="Root"/> are extracted to, out of it.</summary>
    public string Cache => Root + "-cache";

    public static string Pack(params string[] rounds) =>
        $$"""{ "formatVersion": 1, "title": "Pack de test", "rounds": [{{string.Join(", ", rounds)}}] }""";

    public static string Quiz(params string[] questions) =>
        $$"""{ "type": "quiz", "title": "Manche", "questions": [{{string.Join(", ", questions)}}] }""";

    public static string QuestionWithImage(string image) =>
        $$"""{ "text": "Question ?", "image": "{{image.Replace("\\", "\\\\", StringComparison.Ordinal)}}", "choices": [{ "text": "Oui", "correct": true }, { "text": "Non" }] }""";

    /// <summary>
    /// Writes a pack folder.
    /// </summary>
    /// <param name="id">The name of the folder.</param>
    /// <param name="descriptor">The content of <c>pack.json</c>.</param>
    /// <param name="files">Other files of the pack, relative to its folder with <c>/</c> as separator, left empty.</param>
    /// <returns>The folder of the pack.</returns>
    public string Add(string id, string descriptor, params string[] files)
    {
        var folder = Path.Combine(Root, id);
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, PackDescriptor.FileName), descriptor);
        foreach (var file in files)
        {
            var path = Path.Combine(folder, file.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllBytes(path, []);
        }

        return folder;
    }

    /// <summary>
    /// Loads a pack written by <see cref="Add"/>, without any check of a game mode.
    /// </summary>
    public LoadedPack Load(string descriptor, params string[] files) =>
        new PackLoader(NoModeCheck).Load(Add("pack", descriptor, files));

    public static ImmutableArray<PackProblem> NoModeCheck(RoundDescriptor round, string path) => [];

    /// <summary>
    /// A problem as a single line, easy to compare and to read in a failed assertion.
    /// </summary>
    public static string Describe(PackProblem problem) =>
        string.Join(' ', [
            problem.Code.ToString(),
            problem.File,
            problem.Path,
            .. problem.Parameters.OrderBy(parameter => parameter.Key, StringComparer.Ordinal).Select(parameter => $"{parameter.Key}={parameter.Value}"),
        ]);

    public static IEnumerable<string> Describe(LoadedPack pack) => pack.Problems.Select(Describe);

    public void Dispose()
    {
        foreach (var folder in new[] { Root, Cache }.Where(Directory.Exists))
        {
            Directory.Delete(folder, recursive: true);
        }
    }
}
