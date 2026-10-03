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
    /// A valid pack of a single quiz round of <paramref name="questionCount"/> questions, "Question 1 ?" to the last
    /// one, "Oui" (A) being the correct answer of each.
    /// </summary>
    public static string LongQuiz(string title, int questionCount) =>
        Descriptor(
            title,
            [
                new
                {
                    type = "quiz",
                    title = "Longue manche",
                    questions = Enumerable.Range(1, questionCount).Select(number => new
                    {
                        text = $"Question {number} ?",
                        choices = new[] { new { text = "Oui", correct = true }, new { text = "Non", correct = false } },
                    }),
                },
            ]);

    /// <summary>
    /// An invalid pack: the question of its only round has no correct choice.
    /// </summary>
    public static string Broken(string title) => Descriptor(title, [Round("Manche cassée", correct: false)]);

    /// <summary>
    /// A valid pack of a single quiz round, with one question illustrated by each image, in this order.
    /// </summary>
    public static string IllustratedQuiz(string title, params string[] images) =>
        Descriptor(
            title,
            [
                new
                {
                    type = "quiz",
                    title = "Manche illustrée",
                    questions = images.Select(image => new
                    {
                        text = "Question ?",
                        image,
                        choices = new[] { new { text = "Oui", correct = true }, new { text = "Non", correct = false } },
                    }),
                },
            ]);

    /// <summary>
    /// Writes a media file of the pack <paramref name="id"/>, creating its folders.
    /// </summary>
    /// <param name="directory">The pack directory.</param>
    /// <param name="id">The identifier of the pack: the name of its folder.</param>
    /// <param name="media">The path of the file in the pack, with <c>/</c> as separator.</param>
    /// <param name="content">The content of the file.</param>
    public static void WriteMedia(string directory, string id, string media, byte[] content)
    {
        var path = Path.Combine([directory, id, .. media.Split('/')]);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, content);
    }

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
