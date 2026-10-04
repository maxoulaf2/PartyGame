using PartyGame.Contracts;
using PartyGame.Engine.Effects;
using PartyGame.Engine.Inputs;

namespace PartyGame.Engine.Tests.Lobby;

public sealed class SavedGameChoiceTests
{
    /// <summary>What the game master and the phones could send while the server waits for the decision.</summary>
    public static TheoryData<GameInput> OtherInputs =>
    [
        Games.Join("Léa", player: 9),
        Games.Select(Games.PackId),
        Games.Start(),
        Games.ChooseAddress(Games.OtherAddress),
        Games.Loaded(Games.Pack),
        Games.Rename(1, "Zoé2"),
    ];

    [Fact]
    public void CreateResumePending_GameFound_WaitsWithTheVersionOfTheGameFoundAndNobodyRegistered()
    {
        // Given
        var found = Games.InPhase(GamePhase.Round, "Zoé", "Max");

        // When
        var state = Games.Pending(found);

        // Then
        Assert.Equal(GamePhase.ResumePending, state.Phase);
        Assert.Equal(found.Version, state.Version);
        Assert.NotEqual(found.GameId, state.GameId);
        Assert.Empty(state.Players);
        Assert.Empty(state.PlayerTokens);
        Assert.Same(found, state.PendingGame!.Game);
    }

    [Fact]
    public void Handle_ResumeTheGameFound_GoesOnWithItAtTheAddressesOfThisStartup()
    {
        // Given: the game found was advertised on another network
        var found = Games.Accepted(Games.InPhase(GamePhase.Round, "Zoé", "Max"), Games.ChooseAddress(Games.OtherAddress)) with
        {
            JoinAddressCandidates = [new(Games.OtherAddress, "Ancien Wi-Fi")],
        };
        var state = Games.Pending(found);

        // When
        var transition = Games.Engine.Handle(state, Games.Resume(state), Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        Assert.Empty(transition.Effects);
        Assert.Equal(found with { JoinAddress = Games.JoinAddress, JoinAddressCandidates = Games.JoinAddressCandidates }, transition.State);
    }

    [Fact]
    public void Handle_ResumeAnotherGame_IsRejectedAsObsolete()
    {
        // Given
        var state = Games.Pending(Games.LobbyWith("Zoé"));

        // When
        var transition = Games.Engine.Handle(state, new ResumeSavedGame(new GameId(Guid.NewGuid()), Games.Now), Games.Context());

        // Then
        AssertRejected(state, transition, RejectionReason.SavedGameObsolete);
    }

    [Fact]
    public void Handle_ResumeOnceANewGameStarted_IsRejectedAsObsolete()
    {
        // Given: a second console decided first
        var pending = Games.Pending(Games.LobbyWith("Zoé"));
        var state = Games.Accepted(pending, Games.Discard(pending));

        // When
        var transition = Games.Engine.Handle(state, Games.Resume(pending), Games.Context());

        // Then
        AssertRejected(state, transition, RejectionReason.SavedGameObsolete);
    }

    [Fact]
    public void Handle_ResumeWhileMediaFilesAreMissing_IsRejected()
    {
        // Given
        var state = Games.Pending(Games.PlayedUpTo(GamePhase.Round, Games.IllustratedLobbyWith("Zoé")), Games.Flag);

        // When
        var transition = Games.Engine.Handle(state, Games.Resume(state), Games.Context());

        // Then
        AssertRejected(state, transition, RejectionReason.SavedGameMediaMissing);
    }

    [Fact]
    public void Handle_DiscardTheGameFound_StartsTheLobbyAndSetsTheSaveAside()
    {
        // Given
        var state = Games.Pending(Games.InPhase(GamePhase.Round, "Zoé", "Max"));

        // When
        var transition = Games.Engine.Handle(state, Games.Discard(state), Games.Context());

        // Then: nobody registered, the players register again
        Assert.Null(transition.Rejection);
        Assert.Equal([new ArchiveSavedGame()], transition.Effects);
        Assert.Equal(state with { Phase = GamePhase.Lobby, PendingGame = null }, transition.State);
        Assert.Empty(transition.State.Players);
    }

    [Fact]
    public void Handle_DiscardTwice_TheSecondIsRejectedAsObsolete()
    {
        // Given
        var pending = Games.Pending(Games.LobbyWith("Zoé"));
        var state = Games.Accepted(pending, Games.Discard(pending));

        // When
        var transition = Games.Engine.Handle(state, Games.Discard(pending), Games.Context());

        // Then
        AssertRejected(state, transition, RejectionReason.SavedGameObsolete);
    }

    [Fact]
    public void Handle_DiscardWithoutAGameFound_IsRejectedAsObsolete()
    {
        // Given
        var state = Games.LobbyWith("Zoé");

        // When
        var transition = Games.Engine.Handle(state, new DiscardSavedGame(state.GameId, Games.Now), Games.Context());

        // Then
        AssertRejected(state, transition, RejectionReason.SavedGameObsolete);
    }

    [Fact]
    public void Handle_MediaCheckedAndBack_LetsTheGameBeResumed()
    {
        // Given
        var state = Games.Pending(Games.PlayedUpTo(GamePhase.Round, Games.IllustratedLobbyWith("Zoé")), Games.Flag, Games.Monument);

        // When
        var transition = Games.Engine.Handle(state, new SavedGameMediaChecked(state.PendingGame!.Game.GameId, []), Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        Assert.Equal(state with { PendingGame = state.PendingGame with { MissingMedia = [] } }, transition.State);
        Assert.Null(Games.Engine.Handle(transition.State, Games.Resume(state), Games.Context()).Rejection);
    }

    [Fact]
    public void Handle_MediaCheckedStillMissing_IsAcceptedWithoutChange()
    {
        // Given
        var state = Games.Pending(Games.PlayedUpTo(GamePhase.Round, Games.IllustratedLobbyWith("Zoé")), Games.Flag);

        // When
        var transition = Games.Engine.Handle(state, new SavedGameMediaChecked(state.PendingGame!.Game.GameId, [Games.Flag]), Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        Assert.Same(state, transition.State);
    }

    [Fact]
    public void Handle_MediaCheckedOnceDecided_IsRejectedAsObsolete()
    {
        // Given
        var pending = Games.Pending(Games.LobbyWith("Zoé"));
        var state = Games.Accepted(pending, Games.Discard(pending));

        // When
        var transition = Games.Engine.Handle(state, new SavedGameMediaChecked(pending.PendingGame!.Game.GameId, []), Games.Context());

        // Then
        AssertRejected(state, transition, RejectionReason.SavedGameObsolete);
    }

    [Theory]
    [MemberData(nameof(OtherInputs))]
    public void Handle_AnyOtherInputWhileTheGameMasterDecides_IsRejected(GameInput input)
    {
        // Given
        var state = Games.Pending(Games.LobbyWith("Zoé"));

        // When
        var transition = Games.Engine.Handle(state, input, Games.Context());

        // Then
        AssertRejected(state, transition, RejectionReason.GamePending);
    }

    private static void AssertRejected(GameState state, Transition transition, RejectionReason reason)
    {
        Assert.Equal(reason, transition.Rejection);
        Assert.Same(state, transition.State);
        Assert.Empty(transition.Effects);
    }
}
