using PartyGame.Contracts.Packs;
using PartyGame.Engine.Modes.Buzzer;

namespace PartyGame.Engine.Tests.Modes.Buzzer;

public sealed class BuzzerModeTests
{
    private static readonly BuzzerMode _mode = new();

    private static readonly BuzzerRoundDescriptor _round = new()
    {
        Title = "Le plus rapide",
        Questions = [new BuzzerQuestion { Text = "Capitale de la France ?", Answer = "Paris" }],
    };

    [Fact]
    public void Validate_RoundWithinTheConstraintsOfItsDescriptor_ReportsNothing() =>
        Assert.Empty(_mode.Validate(_round, "$.rounds[0]"));

    [Fact]
    public void Start_UntilTheQuestionsArePlayed_FinishesTheRound()
    {
        // When
        var transition = _mode.Start(_round, Games.NewLobby(), Games.Context(42));

        // Then
        Assert.True(transition.IsFinished);
        Assert.IsType<BuzzerRound>(transition.State);
        Assert.Empty(transition.Effects);
    }
}
