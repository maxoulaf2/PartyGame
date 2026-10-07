using PartyGame.Engine.Effects;
using PartyGame.Engine.Inputs;
using PartyGame.Engine.State;
using PartyGame.Engine.Tests.Rounds;

namespace PartyGame.Engine.Tests.Lobby;

public sealed class LobbyReturnTests
{
    public static TheoryData<GamePhase> Started => [GamePhase.Round, GamePhase.BetweenRounds, GamePhase.Finished];

    [Theory]
    [MemberData(nameof(Started))]
    public void Handle_ReturnToLobby_StartsANewGameInTheLobby(GamePhase phase)
    {
        // Given
        var state = Games.InPhase(phase, "Zoé", "Max");

        // When
        var transition = Games.Engine.Handle(state, Games.ReturnToLobby(state), Games.Context());

        // Then: nothing of the game ended remains, but the pack chosen to play it again
        Assert.Null(transition.Rejection);
        var lobby = transition.State;
        Assert.Equal(GamePhase.Lobby, lobby.Phase);
        Assert.NotEqual(state.GameId, lobby.GameId);
        Assert.Null(lobby.Pack);
        Assert.Empty(lobby.Media.Files);
        Assert.Null(lobby.CurrentRound);
        Assert.Equal(state.SelectedPackId, lobby.SelectedPackId);
        Assert.Equal(state.Catalog, lobby.Catalog);
    }

    [Fact]
    public void Handle_ReturnToLobby_KeepsThePlayersWithTheirScoresReset()
    {
        // Given: points awarded, a player who joined once finished, and intents already handled
        var state = Games.InPhase(GamePhase.Round, "Zoé", "Max");
        state = Games.Accepted(state, Games.GameMasterActs(state, FakeGameMasterIntent.Award));
        state = Games.Accepted(state, Games.PlayerActs(state, player: 1, "answers A"));
        state = Games.Accepted(state, Games.SkipRound(state));
        state = Games.NextRoundStarted(state);
        state = Games.Accepted(state, Games.SkipRound(state));
        state = Games.Accepted(state, Games.Join("Léa", player: 3));

        // When
        var lobby = Games.Accepted(state, Games.ReturnToLobby(state));

        // Then: the phones stay registered, and their next intents are still numbered after those handled
        Assert.Equal(state.PlayerTokens, lobby.PlayerTokens);
        Assert.Equal(state.Players.Select(p => (p.Id, p.Nickname, p.IsConnected, p.LastClientSeq)), lobby.Players.Select(p => (p.Id, p.Nickname, p.IsConnected, p.LastClientSeq)));
        Assert.All(lobby.Players, p => Assert.Equal((0, false), (p.Score, p.JoinedAfterEnd)));
    }

    [Fact]
    public void Handle_ReturnToLobbyDuringARound_CancelsTheTimersOfTheRound()
    {
        // Given: the round scheduled a countdown when it started
        var state = Games.InPhase(GamePhase.Round, "Zoé");

        // When
        var transition = Games.Engine.Handle(state, Games.ReturnToLobby(state), Games.Context());

        // Then
        Assert.Equal([new CancelRoundTimers(state.CurrentRound!.Id)], transition.Effects);
    }

    [Fact]
    public void Handle_TimerOfARoundEndedByAReturnToLobby_IsRejected()
    {
        // Given: the countdown elapsed before its cancellation
        var state = Games.InPhase(GamePhase.Round, "Zoé");
        var timer = new TimerElapsed(FakeMode.Countdown, Games.Now + FakeMode.CountdownDuration) { RoundId = state.CurrentRound!.Id };
        var lobby = Games.Accepted(state, Games.ReturnToLobby(state));

        // When
        var transition = Games.Engine.Handle(lobby, timer, Games.Context());

        // Then
        Assert.Same(lobby, transition.State);
        Assert.Equal(RejectionReason.UnexpectedTimer, transition.Rejection);
    }

    [Fact]
    public void Handle_StartAfterAReturnToLobby_PlaysThePackFromItsFirstRound()
    {
        // Given
        var state = Games.InPhase(GamePhase.Finished, "Zoé");
        state = Games.Accepted(state, Games.ReturnToLobby(state));

        // When
        var started = Games.Accepted(state, Games.Start(), seed: 43);

        // Then
        Assert.Equal(GamePhase.RoundIntro, started.Phase);
        Assert.Equal(0, started.CurrentRound!.Index);
    }

    [Fact]
    public void Handle_ReturnToLobbySentAgainOnceInTheNewGame_IsRejected()
    {
        // Given: a double tap, a second console, or a request sent again after a reconnection, once the new game started
        var state = Games.InPhase(GamePhase.Round, "Zoé");
        var request = Games.ReturnToLobby(state);
        state = Games.Accepted(state, request);
        state = Games.Accepted(state, Games.Start(), seed: 43);

        // When
        var transition = Games.Engine.Handle(state, request, Games.Context());

        // Then
        Assert.Same(state, transition.State);
        Assert.Empty(transition.Effects);
        Assert.Equal(RejectionReason.GameMismatch, transition.Rejection);
    }

    [Fact]
    public void Handle_ReturnToLobbyInTheLobby_IsRejected()
    {
        // Given
        var state = Games.LobbyWith("Zoé");

        // When
        var transition = Games.Engine.Handle(state, Games.ReturnToLobby(state), Games.Context());

        // Then
        Assert.Same(state, transition.State);
        Assert.Equal(RejectionReason.GameNotStarted, transition.Rejection);
    }

    [Fact]
    public void Handle_ReturnToLobbyWhileASavedGameIsPending_IsRejected()
    {
        // Given
        var state = Games.Pending(Games.InPhase(GamePhase.Round, "Zoé"));

        // When
        var transition = Games.Engine.Handle(state, new ReturnToLobby(state.GameId, Games.Now), Games.Context());

        // Then
        Assert.Same(state, transition.State);
        Assert.Equal(RejectionReason.GamePending, transition.Rejection);
    }
}
