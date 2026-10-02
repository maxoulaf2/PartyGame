using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.Json;
using PartyGame.Contracts.Packs;
using PartyGame.Engine.Modes.Quiz;
using PartyGame.Tests.Shared;

namespace PartyGame.Engine.Tests.Modes.Quiz;

/// <summary>
/// The sample pack of the repository, played for the exit criterion of phase 2. Until the server loads the packs
/// (US-E06-02), these tests check it the same way: reading, constraints, consistency of the quiz mode and media files.
/// </summary>
public sealed class SampleQuizPackTests
{
    private static readonly string _folder = Path.Combine(RepositoryRoot.Find(), "packs", "quiz-exemple");

    private static readonly PackDescriptor _pack = JsonSerializer.Deserialize<PackDescriptor>(
        File.ReadAllText(Path.Combine(_folder, PackDescriptor.FileName)), PackJsonOptions.Default)!;

    private static IEnumerable<QuizRoundDescriptor> Rounds => _pack.Rounds.Cast<QuizRoundDescriptor>();

    private static IEnumerable<QuizQuestion> Questions => Rounds.SelectMany(round => round.Questions);

    [Fact]
    public void SamplePack_EveryObject_MeetsTheConstraintsOfTheDescriptor()
    {
        object[] objects = [_pack, .. Rounds, .. Questions, .. Questions.SelectMany(question => question.Choices)];

        foreach (var instance in objects)
        {
            Assert.True(Validator.TryValidateObject(instance, new ValidationContext(instance), null, validateAllProperties: true), $"{instance}");
        }
    }

    [Fact]
    public void SamplePack_EveryRound_IsConsistentForTheQuizMode()
    {
        var mode = new QuizMode();

        var problems = _pack.Rounds.SelectMany((round, index) =>
            mode.Validate((QuizRoundDescriptor)round, string.Create(CultureInfo.InvariantCulture, $"$.rounds[{index}]")));

        Assert.Empty(problems);
    }

    [Fact]
    public void SamplePack_EveryImage_ExistsWithTheSameCaseAndASupportedExtension()
    {
        var images = Questions.Where(question => question.Image is not null).Select(question => question.Image!.Value.Value).ToList();
        var files = Directory.GetFiles(_folder, "*", SearchOption.AllDirectories)
            .Select(file => Path.GetRelativePath(_folder, file).Replace('\\', '/'))
            .ToHashSet(StringComparer.Ordinal);

        Assert.NotEmpty(images);
        Assert.All(images, image =>
        {
            Assert.Contains(image, files);
            Assert.Contains(Path.GetExtension(image), (string[])[".jpg", ".jpeg", ".png", ".webp"]);
        });
    }

    [Fact]
    public void SamplePack_Content_CoversWhatTheDemonstrationShows()
    {
        Assert.Equal([5, 5], Rounds.Select(round => round.Questions.Length));
        Assert.Contains(Questions, question => question.Image is not null);
        Assert.Contains(Questions, question => question.AnswerSeconds is not null);
        Assert.Contains(Rounds, round => round.SpeedBonus > 0);
        Assert.Contains(Rounds, round => round.ShuffleChoices);
    }
}
