using PartyGame.Engine.State;

namespace PartyGame.Engine.Tests.Players;

public sealed class RegistrationTests
{
    [Fact]
    public void Handle_JoinGameWithFreeNickname_RegistersPlayerAndToken()
    {
        // Given
        var state = Games.LobbyWith("Zoé");
        var join = Games.Join("Max", player: 2);

        // When
        var transition = Games.Engine.Handle(state, join, Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        Assert.Empty(transition.Effects);
        Assert.Equal([new Player(Games.PlayerIdOf(1), "Zoé", IsConnected: true), new Player(join.PlayerId, "Max", IsConnected: true)], transition.State.Players);
        Assert.Equal(join.PlayerId, transition.State.PlayerTokens[join.Token]);
        Assert.Equal(join.ReconnectionCode, transition.State.ReconnectionCodes[join.PlayerId]);
    }

    [Fact]
    public void Handle_JoinGameWithUntidyNickname_StoresNormalizedNickname()
    {
        // Given
        var state = Games.NewLobby();

        // When
        var transition = Games.Engine.Handle(state, Games.Join("  Jean   Paul "), Games.Context());

        // Then
        Assert.Equal("Jean Paul", Assert.Single(transition.State.Players).Nickname);
    }

    [Fact]
    public void Handle_JoinGameAfterStart_RegistersPlayer()
    {
        // Given
        var state = Games.InPhase(GamePhase.BetweenRounds, "Zoé");

        // When
        var transition = Games.Engine.Handle(state, Games.Join("Max", player: 2), Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        Assert.Equal(GamePhase.BetweenRounds, transition.State.Phase);
        Assert.Equal(2, transition.State.Players.Length);
    }

    [Theory]
    [InlineData("zoe")]
    [InlineData("ZOÉ")]
    [InlineData(" Zoe ")]
    public void Handle_JoinGameWithTakenNickname_IsRejected(string nickname)
    {
        // Given
        var state = Games.LobbyWith("Zoé");

        // When
        var transition = Games.Engine.Handle(state, Games.Join(nickname, player: 2), Games.Context());

        // Then
        Assert.Same(state, transition.State);
        Assert.Empty(transition.Effects);
        Assert.Equal(RejectionReason.NicknameTaken, transition.Rejection);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Seventeen chars!!")]
    [InlineData("Zo\u200Bé")]
    public void Handle_JoinGameWithInvalidNickname_IsRejected(string nickname)
    {
        // Given
        var state = Games.NewLobby();

        // When
        var transition = Games.Engine.Handle(state, Games.Join(nickname), Games.Context());

        // Then
        Assert.Same(state, transition.State);
        Assert.Empty(transition.Effects);
        Assert.Equal(RejectionReason.NicknameInvalid, transition.Rejection);
    }

    [Fact]
    public void Handle_JoinGameReplayed_IsRejectedAsAlreadyJoined()
    {
        // Given
        var join = Games.Join("Zoé");
        var state = Games.Engine.Handle(Games.NewLobby(), join, Games.Context()).State;

        // When
        var transition = Games.Engine.Handle(state, join, Games.Context());

        // Then
        Assert.Same(state, transition.State);
        Assert.Empty(transition.Effects);
        Assert.Equal(RejectionReason.PlayerAlreadyJoined, transition.Rejection);
    }

    [Fact]
    public void Handle_JoinGameWithKnownToken_IsRejectedAsAlreadyJoined()
    {
        // Given
        var state = Games.LobbyWith("Zoé");
        var join = Games.Join("Max", player: 2) with { Token = new PlayerToken("token-1") };

        // When
        var transition = Games.Engine.Handle(state, join, Games.Context());

        // Then
        Assert.Same(state, transition.State);
        Assert.Equal(RejectionReason.PlayerAlreadyJoined, transition.Rejection);
    }

    [Fact]
    public void Handle_JoinGameWithKnownReconnectionCode_IsRejectedAsAlreadyJoined()
    {
        // Given
        var state = Games.LobbyWith("Zoé");
        var join = Games.Join("Max", player: 2) with { ReconnectionCode = state.ReconnectionCodes[Games.PlayerIdOf(1)] };

        // When
        var transition = Games.Engine.Handle(state, join, Games.Context());

        // Then
        Assert.Same(state, transition.State);
        Assert.Equal(RejectionReason.PlayerAlreadyJoined, transition.Rejection);
    }
}
