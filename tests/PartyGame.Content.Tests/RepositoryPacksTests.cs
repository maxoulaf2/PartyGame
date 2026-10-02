using PartyGame.Contracts;
using PartyGame.Contracts.Packs;
using PartyGame.Engine.Modes;
using PartyGame.Engine.Modes.Quiz;
using PartyGame.Tests.Shared;

namespace PartyGame.Content.Tests;

/// <summary>
/// The packs of the repository, used in development, in the tests and for the demonstrations, checked as the server
/// checks them, with the game modes it registers.
/// </summary>
public sealed class RepositoryPacksTests
{
    private static readonly PackLibrary _library =
        new PackLoader(new GameModes([new QuizMode()]).Validate).LoadAll(Path.Combine(RepositoryRoot.Find(), "packs"));

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
    public void EndToEndPacks_AreTheValidAndInvalidPacksTheBrowserTestsExpect()
    {
        // Given: the packs of the Playwright tests (client/e2e/packs), which check what the console shows of them
        var library = new PackLoader(new GameModes([new QuizMode()]).Validate)
            .LoadAll(Path.Combine(RepositoryRoot.Find(), "client", "e2e", "packs"));

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
}
