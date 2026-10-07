using PartyGame.Engine.Inputs;
using PartyGame.Engine.State;

namespace PartyGame.Engine.Tests.Players;

public sealed class RenamingTests
{
    [Fact]
    public void Handle_RenamePlayerWithFreeNickname_RenamesOnlyThatPlayer()
    {
        // Given
        var state = Games.LobbyWith("Zoé", "Max");

        // When
        var transition = Games.Engine.Handle(state, Games.Rename(2, "  Maxime   B "), Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        Assert.Empty(transition.Effects);
        Assert.Equal(
            [new Player(Games.PlayerIdOf(1), "Zoé", IsConnected: true), new Player(Games.PlayerIdOf(2), "Maxime B", IsConnected: true)],
            transition.State.Players);
        Assert.Equal(state.PlayerTokens, transition.State.PlayerTokens);
    }

    [Fact]
    public void Handle_RenameDisconnectedPlayer_RenamesAndKeepsThemDisconnected()
    {
        // Given
        var state = Games.LobbyWith("Zoé");
        state = Games.Engine.Handle(state, new PlayerConnectionLost(Games.PlayerIdOf(1)), Games.Context()).State;

        // When
        var transition = Games.Engine.Handle(state, Games.Rename(1, "Zoë"), Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        Assert.Equal(new Player(Games.PlayerIdOf(1), "Zoë", IsConnected: false), Assert.Single(transition.State.Players));
    }

    [Fact]
    public void Handle_RenamePlayerAfterStart_RenamesPlayer()
    {
        // Given
        var state = Games.InPhase(GamePhase.Finished, "Zoé");

        // When
        var transition = Games.Engine.Handle(state, Games.Rename(1, "Léa"), Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        Assert.Equal(GamePhase.Finished, transition.State.Phase);
        Assert.Equal("Léa", Assert.Single(transition.State.Players).Nickname);
    }

    [Theory]
    [InlineData("zoe")]
    [InlineData("ZOÉ")]
    public void Handle_RenamePlayerToVariantOfOwnNickname_IsAcceptedAndChangesIt(string nickname)
    {
        // Given
        var state = Games.LobbyWith("Zoé", "Max");

        // When
        var transition = Games.Engine.Handle(state, Games.Rename(1, nickname), Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        Assert.Equal([nickname, "Max"], transition.State.Players.Select(p => p.Nickname));
    }

    [Theory]
    [InlineData("Zoé")]
    [InlineData("  Zoé ")]
    public void Handle_RenamePlayerToOwnNickname_IsAcceptedWithoutChange(string nickname)
    {
        // Given
        var state = Games.LobbyWith("Zoé", "Max");

        // When
        var transition = Games.Engine.Handle(state, Games.Rename(1, nickname), Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        Assert.Same(state, transition.State);
        Assert.Empty(transition.Effects);
    }

    [Theory]
    [InlineData("Max")]
    [InlineData("max")]
    [InlineData(" MAX ")]
    public void Handle_RenamePlayerToTakenNickname_IsRejected(string nickname)
    {
        // Given
        var state = Games.LobbyWith("Zoé", "Max");

        // When
        var transition = Games.Engine.Handle(state, Games.Rename(1, nickname), Games.Context());

        // Then
        Assert.Same(state, transition.State);
        Assert.Empty(transition.Effects);
        Assert.Equal(RejectionReason.NicknameTaken, transition.Rejection);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Seventeen chars!!")]
    [InlineData("Zo​é")]
    [InlineData("Zo\u0007é")]
    public void Handle_RenamePlayerToInvalidNickname_IsRejected(string nickname)
    {
        // Given
        var state = Games.LobbyWith("Zoé");

        // When
        var transition = Games.Engine.Handle(state, Games.Rename(1, nickname), Games.Context());

        // Then
        Assert.Same(state, transition.State);
        Assert.Empty(transition.Effects);
        Assert.Equal(RejectionReason.NicknameInvalid, transition.Rejection);
    }

    [Fact]
    public void Handle_RenameUnknownPlayer_IsRejected()
    {
        // Given
        var state = Games.LobbyWith("Zoé");

        // When
        var transition = Games.Engine.Handle(state, Games.Rename(9, "Max"), Games.Context());

        // Then
        Assert.Same(state, transition.State);
        Assert.Empty(transition.Effects);
        Assert.Equal(RejectionReason.PlayerUnknown, transition.Rejection);
    }
}
