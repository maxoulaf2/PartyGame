using System.Text.RegularExpressions;
using PartyGame.Contracts;
using PartyGame.Contracts.Packs;
using PartyGame.Engine.Modes;
using PartyGame.Engine.Modes.BlindTest;
using PartyGame.Engine.Modes.Buzzer;
using PartyGame.Engine.Modes.OpenQuestion;
using PartyGame.Engine.Modes.Quiz;
using PartyGame.Tests.Shared;

namespace PartyGame.Content.Tests;

/// <summary>
/// The packs of the repository, used in development, in the tests and for the demonstrations, checked as the server
/// checks them, with the game modes it registers.
/// </summary>
public sealed partial class RepositoryPacksTests
{
    // The repository holds no zip pack: nothing is ever extracted there.
    private static readonly string _noCache = Path.Combine(Path.GetTempPath(), "partygame-tests", "no-cache");

    private static readonly GameModes _modes = new([new QuizMode(), new BuzzerMode(), new BlindTestMode(), new OpenQuestionMode()]);

    private static readonly PackLibrary _library = new PackLoader(_modes.Validate).LoadAll(Path.Combine(RepositoryRoot.Find(), "packs"), _noCache);

    [Fact]
    public void LoadAll_RepositoryPacks_AreAllValid()
    {
        Assert.NotEmpty(_library.Packs);
        Assert.All(_library.Packs, pack => Assert.True(pack.IsValid, $"{pack.Id}: {string.Join(Environment.NewLine, TestPacks.Describe(pack))}"));
    }

    [Fact]
    public void SampleQuizPack_Content_CoversWhatTheDemonstrationShows()
    {
        var pack = Assert.Single(_library.Packs, pack => pack.Id == "quiz-exemple");
        Assert.True(pack.IsValid);
        var rounds = pack.Descriptor.Rounds.Cast<QuizRoundDescriptor>().ToList();
        var questions = rounds.SelectMany(round => round.Questions).ToList();

        Assert.Equal([5, 5], rounds.Select(round => round.Questions.Length));
        Assert.Contains(questions, question => question.Image is not null);
        Assert.Contains(questions, question => question.AnswerSeconds is not null);
        Assert.Contains(rounds, round => round.SpeedBonus > 0);
        Assert.Contains(rounds, round => round.ShuffleChoices);
    }

    [Fact]
    public void SampleBuzzerPack_Content_CoversWhatTheDemonstrationShows()
    {
        var pack = Assert.Single(_library.Packs, pack => pack.Id == "buzzer-exemple");
        Assert.True(pack.IsValid);
        var round = Assert.IsType<BuzzerRoundDescriptor>(Assert.Single(pack.Descriptor.Rounds));

        Assert.Equal(10, round.Questions.Length);
        Assert.Contains(round.Questions, question => question.Image is not null);
    }

    [Fact]
    public void SampleBlindTestPack_Content_CoversWhatTheDemonstrationShows()
    {
        var pack = Assert.Single(_library.Packs, pack => pack.Id == "blindtest-exemple");
        Assert.True(pack.IsValid);
        var round = Assert.IsType<BlindTestRoundDescriptor>(Assert.Single(pack.Descriptor.Rounds));

        Assert.Equal(10, round.Tracks.Length);
        Assert.Contains(round.Tracks, track => track.Artist is null);
        Assert.Contains(round.Tracks, track => track.Image is not null);
        Assert.Contains(round.Tracks, track => track.Excerpt.Start > 0);
        Assert.True(File.Exists(Path.Combine(pack.Folder, "LICENCE.md")));
    }

    [Fact]
    public void SampleOpenQuestionPack_Content_CoversWhatTheDemonstrationShows()
    {
        var pack = Assert.Single(_library.Packs, pack => pack.Id == "openquestion-exemple");
        Assert.True(pack.IsValid);
        var round = Assert.IsType<OpenQuestionRoundDescriptor>(Assert.Single(pack.Descriptor.Rounds));

        Assert.Equal(10, round.Questions.Length);
        Assert.True(round.SpeedBonus > 0);
        Assert.Contains(round.Questions, question => question.Image is not null);
        Assert.Contains(round.Questions, question => question.AnswerSeconds is not null);
        Assert.Contains(round.Questions, question => !question.AcceptedAnswers.IsEmpty);
        Assert.Contains(round.Questions, question => question.InputMode == OpenQuestionInputMode.Numeric);
    }

    [Fact]
    public void SampleEveningPack_Content_PlaysOneRoundOfEachMode()
    {
        var pack = Assert.Single(_library.Packs, pack => pack.Id == "soiree-exemple");
        Assert.True(pack.IsValid);

        Assert.Equal(
            [typeof(QuizRoundDescriptor), typeof(OpenQuestionRoundDescriptor), typeof(BuzzerRoundDescriptor), typeof(BlindTestRoundDescriptor)],
            pack.Descriptor.Rounds.Select(round => round.GetType()));
        Assert.True(File.Exists(Path.Combine(pack.Folder, "LICENCE.md")));
    }

    [Theory]
    [InlineData("quiz")]
    [InlineData("buzzer")]
    [InlineData("blindtest")]
    [InlineData("openquestion")]
    public void ModeDocumentation_Example_IsAValidPack(string mode)
    {
        // Given: the complete example of docs/modes/<mode>.md, in a copy of the sample pack of the mode, whose media it uses.
        // The loader checks the structure and bounds the schema is generated from, then the media and the consistency.
        var page = File.ReadAllText(Path.Combine(RepositoryRoot.Find(), "docs", "modes", $"{mode}.md"));
        var example = Assert.Single(JsonBlock().Matches(page)).Groups[1].Value;
        var sample = Path.Combine(RepositoryRoot.Find(), "packs", $"{mode}-exemple");
        using var packs = new TestPacks();
        var folder = packs.Add(mode, example);
        foreach (var media in Directory.EnumerateFiles(sample, "*", SearchOption.AllDirectories).Where(file => Path.GetFileName(file) != PackDescriptor.FileName))
        {
            var copy = Path.Combine(folder, Path.GetRelativePath(sample, media));
            Directory.CreateDirectory(Path.GetDirectoryName(copy)!);
            File.Copy(media, copy);
        }

        // When
        var pack = new PackLoader(_modes.Validate).Load(folder);

        // Then
        Assert.True(pack.IsValid, string.Join(Environment.NewLine, TestPacks.Describe(pack)));
        Assert.All(pack.Descriptor.Rounds, round => Assert.Equal(mode, GameModes.TypeOf(round)));
    }

    [Fact]
    public void EndToEndPacks_AreTheValidAndInvalidPacksTheBrowserTestsExpect()
    {
        // Given: the packs of the Playwright tests (client/e2e/packs), which check what the console shows of them
        var library = new PackLoader(new GameModes([new QuizMode()]).Validate)
            .LoadAll(Path.Combine(RepositoryRoot.Find(), "client", "e2e", "packs"), _noCache);

        // Then
        Assert.Equal(
            [("apero", true), ("casse", false), ("soiree", true)],
            library.Packs.Select(pack => (pack.Id, pack.IsValid)));
        var soiree = library.Packs[2];
        Assert.Equal("Grande soirée", soiree.Title);
        Assert.Equal(["Échauffement", "Finale"], soiree.Descriptor!.Rounds.Select(r => r.Title));
        var broken = library.Packs[1];
        Assert.Equal(
            [
                (PackProblemCode.PackMediaMissing, "$.rounds[0].questions[0].image"),
                (PackProblemCode.QuizCorrectChoiceMissing, "$.rounds[0].questions[0]"),
            ],
            broken.Problems.Select(problem => (problem.Code, problem.Path)).Order());
        Assert.Equal("images/tour-eiffel.jpg", broken.Problems.Single(p => p.Code == PackProblemCode.PackMediaMissing).Parameters["media"]);
    }

    [GeneratedRegex(@"^```json
?
(.*?)^```", RegexOptions.Multiline | RegexOptions.Singleline)]
    private static partial Regex JsonBlock();
}
