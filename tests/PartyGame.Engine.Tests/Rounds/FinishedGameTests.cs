using PartyGame.Engine.Inputs;

namespace PartyGame.Engine.Tests.Rounds;

/// <summary>
/// Once the last round is over, the game is finished: it shows its final ranking, and nothing makes it go on.
/// </summary>
public sealed class FinishedGameTests
{
    /// <summary>Every intent that plays the game, as a phone or a console would still send it once the game is finished.</summary>
    public static TheoryData<string, RejectionReason> GameIntents => new()
    {
        { "player round intent", RejectionReason.NotInRound },
        { "game master round intent", RejectionReason.NotInRound },
        { "next round", RejectionReason.NotBetweenRounds },
        { "start", RejectionReason.GameAlreadyStarted },
        { "pack choice", RejectionReason.GameAlreadyStarted },
    };

    [Fact]
    public void Handle_LastRoundFinished_KeepsEveryPointForTheFinalRanking()
    {
        // Given: the points of the last question of the last round, awarded at its reveal
        var state = Games.InPhase(GamePhase.BetweenRounds, "Zoé", "Max");
        state = Games.NextRoundStarted(state);
        state = Games.WithScores(state, 2000, 1000);
        state = Games.Accepted(state, Games.GameMasterActs(state, FakeGameMasterIntent.Award));

        // When
        var transition = Games.Engine.Handle(state, Games.GameMasterActs(state, FakeGameMasterIntent.Finish), Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        Assert.Equal(GamePhase.Finished, transition.State.Phase);
        Assert.Equal([2000 + FakeMode.AwardedPoints, 1000 + FakeMode.AwardedPoints], transition.State.Players.Select(p => p.Score));
    }

    [Theory]
    [MemberData(nameof(GameIntents))]
    public void Handle_GameIntentOnceFinished_IsRejected(string intent, RejectionReason expected)
    {
        // Given: intents that name the last round, the one the game finished with
        var state = Games.InPhase(GamePhase.Finished, "Zoé", "Max");
        var lastRound = state.CurrentRound!.Id;
        GameInput input = intent switch
        {
            "player round intent" => new PlayerRoundInput(Games.PlayerIdOf(1), ClientSeq: 1, new FakePlayerIntent(lastRound, "answers A"), Games.Now),
            "game master round intent" => new GameMasterRoundInput(new FakeGameMasterIntent(lastRound, FakeGameMasterIntent.Finish), Games.Now),
            "next round" => new NextRound(lastRound, Games.Now),
            "start" => Games.Start(),
            "pack choice" => Games.Select(Games.PackId),
            _ => throw new ArgumentOutOfRangeException(nameof(intent)),
        };

        // When
        var transition = Games.Engine.Handle(state, input, Games.Context());

        // Then
        Assert.Same(state, transition.State);
        Assert.Empty(transition.Effects);
        Assert.Equal(expected, transition.Rejection);
    }

    [Fact]
    public void Handle_JoinGameOnceFinished_RegistersPlayerWhoJoinedAfterTheEnd()
    {
        // Given
        var state = Games.InPhase(GamePhase.Finished, "Zoé");

        // When
        var transition = Games.Engine.Handle(state, Games.Join("Max", player: 2), Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        Assert.Equal(GamePhase.Finished, transition.State.Phase);
        Assert.Equal([false, true], transition.State.Players.Select(p => p.JoinedAfterEnd));
    }

    [Theory]
    [InlineData(GamePhase.Lobby)]
    [InlineData(GamePhase.Round)]
    [InlineData(GamePhase.BetweenRounds)]
    public void Handle_JoinGameBeforeTheEnd_RegistersPlayerWhoPlays(GamePhase phase)
    {
        // Given
        var state = Games.InPhase(phase, "Zoé");

        // When
        var transition = Games.Engine.Handle(state, Games.Join("Max", player: 2), Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        Assert.False(transition.State.Players[1].JoinedAfterEnd);
    }
}
