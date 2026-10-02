using System.Text.Json;
using PartyGame.Contracts.Packs;

namespace PartyGame.Server.Tests.Packs;

/// <summary>
/// Writes the packs a test server loads from its pack directory.
/// </summary>
internal static class TestPacks
{
    /// <summary>
    /// A valid pack of quiz rounds with the given titles, each of a single question.
    /// </summary>
    public static string Quiz(string title, params string[] roundTitles) =>
        Descriptor(title, roundTitles.Select(round => Round(round, correct: true)));

    /// <summary>
    /// An invalid pack: the question of its only round has no correct choice.
    /// </summary>
    public static string Broken(string title) => Descriptor(title, [Round("Manche cassée", correct: false)]);

    /// <summary>
    /// Writes <paramref name="descriptor"/> as the <c>pack.json</c> of the pack <paramref name="id"/>, creating its folder.
    /// </summary>
    public static void Write(string directory, string id, string descriptor)
    {
        var folder = Path.Combine(directory, id);
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, PackDescriptor.FileName), descriptor);
    }

    private static string Descriptor(string title, IEnumerable<object> rounds) =>
        JsonSerializer.Serialize(new { formatVersion = 1, title, rounds });

    private static object Round(string title, bool correct) =>
        new
        {
            type = "quiz",
            title,
            questions = new[]
            {
                new { text = "Question ?", choices = new[] { new { text = "Oui", correct }, new { text = "Non", correct = false } } },
            },
        };
}
