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
}
