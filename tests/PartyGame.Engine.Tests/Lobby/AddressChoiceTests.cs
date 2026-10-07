using PartyGame.Engine.State;

namespace PartyGame.Engine.Tests.Lobby;

public sealed class AddressChoiceTests
{
    [Fact]
    public void Handle_ChooseAnotherCandidate_AdvertisesItAndKeepsTheCandidates()
    {
        // Given
        var state = Games.LobbyWith("Zoé");

        // When
        var transition = Games.Engine.Handle(state, Games.ChooseAddress(Games.OtherAddress), Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        Assert.Empty(transition.Effects);
        Assert.Equal(state with { JoinAddress = Games.OtherAddress }, transition.State);
    }

    [Fact]
    public void Handle_ChooseTheAdvertisedAddress_IsAcceptedWithoutChange()
    {
        // Given
        var state = Games.NewLobby();

        // When
        var transition = Games.Engine.Handle(state, Games.ChooseAddress(Games.JoinAddress), Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        Assert.Same(state, transition.State);
        Assert.Empty(transition.Effects);
    }

    [Fact]
    public void Handle_ChooseDuringARound_AdvertisesTheAddressAndKeepsTheRound()
    {
        // Given: registration stays open once the game is started
        var state = Games.InPhase(GamePhase.Round, "Zoé");

        // When
        var transition = Games.Engine.Handle(state, Games.ChooseAddress(Games.OtherAddress), Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        Assert.Equal((GamePhase.Round, Games.OtherAddress), (transition.State.Phase, transition.State.JoinAddress));
    }

    [Fact]
    public void Handle_ChooseWhenNoAddressIsAdvertised_AdvertisesTheCandidate()
    {
        // Given
        var state = Games.NewLobby() with { JoinAddress = null };

        // When
        var transition = Games.Engine.Handle(state, Games.ChooseAddress(Games.OtherAddress), Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        Assert.Equal(Games.OtherAddress, transition.State.JoinAddress);
    }

    [Theory]
    [InlineData("192.168.1.99")]
    [InlineData("example.com")]
    [InlineData("")]
    [InlineData(" 10.0.0.2")]
    public void Handle_ChooseAnAddressThatIsNoCandidate_IsRejected(string address)
    {
        // Given
        var state = Games.NewLobby();

        // When
        var transition = Games.Engine.Handle(state, Games.ChooseAddress(address), Games.Context());

        // Then
        Assert.Same(state, transition.State);
        Assert.Empty(transition.Effects);
        Assert.Equal(RejectionReason.AddressUnknown, transition.Rejection);
    }

    [Fact]
    public void Handle_ChooseWithoutAnyCandidate_IsRejected()
    {
        // Given
        var state = Games.NewLobby() with { JoinAddress = null, JoinAddressCandidates = [] };

        // When
        var transition = Games.Engine.Handle(state, Games.ChooseAddress(Games.JoinAddress), Games.Context());

        // Then
        Assert.Same(state, transition.State);
        Assert.Equal(RejectionReason.AddressUnknown, transition.Rejection);
    }
}
