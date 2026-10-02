using PartyGame.Contracts;
using PartyGame.Contracts.Packs;
using PartyGame.Engine.Modes;
using PartyGame.Engine.Modes.Quiz;
using PartyGame.Engine.Tests.Rounds;

namespace PartyGame.Engine.Tests.Modes;

public sealed class GameModesTests
{
    [Fact]
    public void TryFind_RegisteredActivity_FindsItsMode()
    {
        // Given
        var mode = new FakeMode();
        var modes = new GameModes([mode]);

        // When
        var found = modes.TryFind(Games.TwoRounds[0], out var foundMode);

        // Then
        Assert.True(found);
        Assert.Same(mode, foundMode);
        Assert.Same(mode, modes.For(Games.TwoRounds[0]));
    }

    [Fact]
    public void TryFind_ActivityWithoutMode_FindsNothing()
    {
        // Given
        var modes = new GameModes([new FakeMode()]);
        var quiz = new QuizRoundDescriptor { Title = "Quiz", Questions = [] };

        // When
        var found = modes.TryFind(quiz, out var mode);

        // Then
        Assert.False(found);
        Assert.Null(mode);
        Assert.Throws<InvalidOperationException>(() => modes.For(quiz));
    }

    [Fact]
    public void Validate_RegisteredActivity_ReturnsTheProblemsOfItsMode()
    {
        // Given
        var modes = new GameModes([new QuizMode()]);
        var quiz = new QuizRoundDescriptor
        {
            Title = "Quiz",
            Questions = [new QuizQuestion { Text = "Question ?", Choices = [new QuizChoice { Text = "Oui" }, new QuizChoice { Text = "Non" }] }],
        };

        // When
        var problems = modes.Validate(quiz, "$.rounds[2]");

        // Then
        var problem = Assert.Single(problems);
        Assert.Equal(PackProblemCode.QuizCorrectChoiceMissing, problem.Code);
        Assert.Equal("$.rounds[2].questions[0]", problem.Path);
    }

    [Fact]
    public void Validate_ActivityWithoutMode_ReportsItsTypeAsUnknown()
    {
        // Given
        var modes = new GameModes([new FakeMode()]);
        var quiz = new QuizRoundDescriptor { Title = "Quiz", Questions = [] };

        // When
        var problems = modes.Validate(quiz, "$.rounds[1]");

        // Then
        var problem = Assert.Single(problems);
        Assert.Equal(PackProblemCode.PackRoundTypeUnknown, problem.Code);
        Assert.Equal(PackDescriptor.FileName, problem.File);
        Assert.Equal("$.rounds[1].type", problem.Path);
        Assert.Equal("quiz", problem.Parameters["type"]);
    }

    [Fact]
    public void Constructor_TwoModesForTheSameActivity_Throws()
    {
        // When
        var create = () => new GameModes([new FakeMode(), new FakeMode()]);

        // Then
        var exception = Assert.Throws<ArgumentException>(create);
        Assert.Contains(nameof(FakeRoundDescriptor), exception.Message, StringComparison.Ordinal);
    }
}
