using PartyGame.Contracts.Packs;
using PartyGame.Engine.Modes;
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
    public void Constructor_TwoModesForTheSameActivity_Throws()
    {
        // When
        var create = () => new GameModes([new FakeMode(), new FakeMode()]);

        // Then
        var exception = Assert.Throws<ArgumentException>(create);
        Assert.Contains(nameof(FakeRoundDescriptor), exception.Message, StringComparison.Ordinal);
    }
}
