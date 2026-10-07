using PartyGame.Engine.Inputs;
using PartyGame.Engine.State;

namespace PartyGame.Engine.Tests.Lobby;

public sealed class JoinCodeVisibilityTests
{
    [Fact]
    public void Handle_ShowDuringARound_ShowsTheCodeAndKeepsTheRound()
    {
        // Given
        var state = Games.InPhase(GamePhase.Round, "Zoé");

        // When
        var transition = Games.Engine.Handle(state, new ShowJoinCode(true, Games.Now), Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        Assert.Empty(transition.Effects);
        Assert.Equal(state with { JoinCodeShown = true }, transition.State);
    }

    [Fact]
    public void Handle_HideAShownCode_HidesIt()
    {
        // Given
        var state = Games.InPhase(GamePhase.BetweenRounds, "Zoé") with { JoinCodeShown = true };

        // When
        var transition = Games.Engine.Handle(state, new ShowJoinCode(false, Games.Now), Games.Context());

        // Then
        Assert.False(transition.State.JoinCodeShown);
    }

    [Fact]
    public void Handle_ShowAShownCodeAgain_IsAcceptedWithoutChange()
    {
        // Given: a double tap, or a second console
        var state = Games.InPhase(GamePhase.Round, "Zoé") with { JoinCodeShown = true };

        // When
        var transition = Games.Engine.Handle(state, new ShowJoinCode(true, Games.Now), Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        Assert.Same(state, transition.State);
    }
}
