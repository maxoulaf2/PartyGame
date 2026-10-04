using PartyGame.Contracts;
using PartyGame.Contracts.Packs;
using static PartyGame.Engine.Tests.Modes.Quiz.QuizGames;

namespace PartyGame.Engine.Tests.Modes.Quiz;

public sealed class QuizMediaTests
{
    private static readonly MediaPath _monument = new("images/tour-eiffel.jpg");

    [Fact]
    public void StepOf_QuestionInProgress_GivesItsNumberAmongTheQuestionsOfTheRound()
    {
        // Given
        var state = AtQuestion(Started([Round(IllustratedQuestion, CapitalQuestion, LastQuestion)], ["Zoé"]), 1);

        // When
        var step = Mode.StepOf(RoundOf(state));

        // Then
        Assert.Equal(new RoundStep(2, 3), step);
    }

    [Fact]
    public void LocateMedia_ImageOfTheQuestionInProgress_GivesItsNumber()
    {
        // Given: the image illustrates the first question as well
        var state = AtQuestion(Started([Round(IllustratedQuestion, CapitalQuestion, IllustratedQuestion)], ["Zoé"]), 2);

        // When
        var step = Mode.LocateMedia(RoundOf(state), Flag);

        // Then: the TV screen shows the image of the question in progress only
        Assert.Equal(3, step);
    }

    [Fact]
    public void LocateMedia_ImageOfAnotherQuestion_GivesTheFirstOneItIllustrates()
    {
        // Given
        var state = AtQuestion(Started([Round(CapitalQuestion, IllustratedQuestion, IllustratedQuestion)], ["Zoé"]), 0);

        // When
        var step = Mode.LocateMedia(RoundOf(state), Flag);

        // Then
        Assert.Equal(2, step);
    }

    [Fact]
    public void LocateMedia_ImageOfNoQuestionOfTheRound_GivesNothing()
    {
        // Given
        var state = Started([Round(CapitalQuestion, LastQuestion)], ["Zoé"]);

        // When
        var step = Mode.LocateMedia(RoundOf(state), _monument);

        // Then
        Assert.Null(step);
    }

    [Fact]
    public void Locate_ImageOfTheQuestionInProgress_GivesTheRoundAndTheQuestion()
    {
        // Given
        var state = AtQuestion(Started([Round(CapitalQuestion, IllustratedQuestion)], ["Zoé"]), 1);
        var id = state.Media.Files.Single(file => file.Value == Flag).Key;

        // When
        var location = new MediaLocator(QuizGames.Modes).Locate(state, id);

        // Then
        Assert.Equal(new MediaLocation(Flag, Engine.Projections.Snapshots.RoundInfoOf(state), Step: 2), location);
    }
}
