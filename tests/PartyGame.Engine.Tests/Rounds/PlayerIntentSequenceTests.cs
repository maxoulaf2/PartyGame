using PartyGame.Engine.Inputs;
using PartyGame.Engine.State;

namespace PartyGame.Engine.Tests.Rounds;

/// <summary>
/// The numbers phones give the intents of their player, so that one sent again after a lost connection is never handled
/// twice, whatever the game mode.
/// </summary>
public sealed class PlayerIntentSequenceTests
{
    [Fact]
    public void Handle_PlayerIntentAccepted_RecordsItsClientSeq()
    {
        // Given
        var state = Games.InPhase(GamePhase.Round, "Zoé", "Max");

        // When
        var transition = Games.Engine.Handle(state, Acts(state, clientSeq: 3, "answers A"), Games.Context());

        // Then: the number is the player's own, and the mode got the intent
        Assert.Null(transition.Rejection);
        Assert.Equal([3L, 0L], transition.State.Players.Select(p => p.LastClientSeq));
        Assert.Contains($"player {Games.PlayerIdOf(1).Value} answers A", ((FakeRoundState)transition.State.CurrentRound!.State!).Inputs);
    }

    [Fact]
    public void Handle_PlayerIntentThatScores_AwardsThePointsAndRecordsItsClientSeq()
    {
        // Given: a mode that awards points as soon as a player acts, as a buzzer would
        var state = Games.InPhase(GamePhase.Round, "Zoé", "Max");

        // When
        var transition = Games.Engine.Handle(state, Acts(state, clientSeq: 3, FakePlayerIntent.Scores), Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        Assert.Equal([(3L, FakeMode.AwardedPoints), (0L, 0)], transition.State.Players.Select(p => (p.LastClientSeq, p.Score)));
    }

    [Theory]
    [InlineData(3)]
    [InlineData(2)]
    public void Handle_PlayerIntentNumberedUpToTheLastAccepted_IsRejectedWithoutReachingTheMode(long clientSeq)
    {
        // Given: intent 3 of Zoé was accepted, and her phone sends it again, or an older one
        var first = Games.InPhase(GamePhase.Round, "Zoé");
        var state = Games.Accepted(first, Acts(first, clientSeq: 3, "answers A"));

        // When
        var transition = Games.Engine.Handle(state, Acts(state, clientSeq, "answers A"), Games.Context());

        // Then
        Assert.Same(state, transition.State);
        Assert.Empty(transition.Effects);
        Assert.Equal(RejectionReason.IntentAlreadyHandled, transition.Rejection);
    }

    [Fact]
    public void Handle_PlayerIntentNumberedAfterTheLastAccepted_IsHandled()
    {
        // Given
        var first = Games.InPhase(GamePhase.Round, "Zoé");
        var state = Games.Accepted(first, Acts(first, clientSeq: 1, "answers A"));

        // When: numbers may skip some, such as those of intents rejected meanwhile
        var transition = Games.Engine.Handle(state, Acts(state, clientSeq: 5, "answers B"), Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        Assert.Equal(5, Assert.Single(transition.State.Players).LastClientSeq);
    }

    [Fact]
    public void Handle_PlayerIntentTheModeAcceptsWithoutChange_RecordsItsClientSeq()
    {
        // Given
        var state = Games.InPhase(GamePhase.Round, "Zoé");

        // When
        var transition = Games.Engine.Handle(state, Acts(state, clientSeq: 1, FakePlayerIntent.Nothing), Games.Context());

        // Then: the intent counts as handled, so that the same one sent again is not handled again
        Assert.Null(transition.Rejection);
        Assert.Equal(1, Assert.Single(transition.State.Players).LastClientSeq);
        Assert.Same(state.CurrentRound, transition.State.CurrentRound);
    }

    [Fact]
    public void Handle_PlayerIntentTheModeRejects_DoesNotRecordItsClientSeq()
    {
        // Given
        var state = Games.InPhase(GamePhase.Round, "Zoé");

        // When
        var transition = Games.Engine.Handle(state, Acts(state, clientSeq: 1, FakePlayerIntent.Refused), Games.Context());

        // Then: the state is the same instance, and nothing is broadcast
        Assert.Same(state, transition.State);
        Assert.Equal(RejectionReason.PhaseMismatch, transition.Rejection);
    }

    [Fact]
    public void Handle_PlayerIntentOfAnotherPlayerWithTheSameNumber_IsHandled()
    {
        // Given: intent 1 of Zoé was accepted
        var first = Games.InPhase(GamePhase.Round, "Zoé", "Max");
        var state = Games.Accepted(first, Acts(first, clientSeq: 1, "answers A"));

        // When: the first intent of Max
        var transition = Games.Engine.Handle(state, Acts(state, clientSeq: 1, "answers B", player: 2), Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        Assert.Equal([1L, 1L], transition.State.Players.Select(p => p.LastClientSeq));
    }

    [Fact]
    public void Handle_PlayerIntentOfAnUnknownPlayer_IsRejected()
    {
        // Given
        var state = Games.InPhase(GamePhase.Round, "Zoé");

        // When
        var transition = Games.Engine.Handle(state, Acts(state, clientSeq: 1, "answers A", player: 9), Games.Context());

        // Then
        Assert.Same(state, transition.State);
        Assert.Equal(RejectionReason.PlayerUnknown, transition.Rejection);
    }

    private static PlayerRoundInput Acts(GameState state, long clientSeq, string action, int player = 1) =>
        Games.PlayerActs(state, player, action) with { ClientSeq = clientSeq };
}
