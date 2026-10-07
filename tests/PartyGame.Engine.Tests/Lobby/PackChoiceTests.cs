using PartyGame.Engine.Packs;
using PartyGame.Engine.State;

namespace PartyGame.Engine.Tests.Lobby;

public sealed class PackChoiceTests
{
    public static TheoryData<GamePhase> Started => [GamePhase.Round, GamePhase.BetweenRounds, GamePhase.Finished];

    private static readonly CatalogPack _other = Games.ValidPack("autre", "Autre soirée", Games.TwoRounds);

    [Fact]
    public void Handle_SelectAnotherValidPack_SelectsItAndKeepsTheCatalog()
    {
        // Given
        var state = LobbyWith(Games.Pack, _other);

        // When
        var transition = Games.Engine.Handle(state, Games.Select(_other.Id), Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        Assert.Empty(transition.Effects);
        Assert.Equal(state with { SelectedPackId = _other.Id }, transition.State);
    }

    [Fact]
    public void Handle_SelectTheSelectedPack_IsAcceptedWithoutChange()
    {
        // Given: a double tap, or a second game master
        var state = Games.LobbyWith("Zoé");

        // When
        var transition = Games.Engine.Handle(state, Games.Select(Games.PackId), Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        Assert.Same(state, transition.State);
        Assert.Empty(transition.Effects);
    }

    [Theory]
    [InlineData("inconnu")]
    [InlineData("SOIREE")]
    [InlineData("")]
    public void Handle_SelectAPackThatIsNotInTheCatalog_IsRejected(string packId)
    {
        // Given
        var state = Games.LobbyWith("Zoé");

        // When
        var transition = Games.Engine.Handle(state, Games.Select(packId), Games.Context());

        // Then
        Assert.Same(state, transition.State);
        Assert.Empty(transition.Effects);
        Assert.Equal(RejectionReason.PackUnknown, transition.Rejection);
    }

    [Fact]
    public void Handle_SelectAnInvalidPack_IsRejected()
    {
        // Given
        var state = LobbyWith(Games.Pack, Games.InvalidPack("casse"));

        // When
        var transition = Games.Engine.Handle(state, Games.Select("casse"), Games.Context());

        // Then
        Assert.Same(state, transition.State);
        Assert.Empty(transition.Effects);
        Assert.Equal(RejectionReason.PackInvalid, transition.Rejection);
    }

    [Theory]
    [MemberData(nameof(Started))]
    public void Handle_SelectOnceStarted_IsRejected(GamePhase phase)
    {
        // Given: the pack of a started game is fixed
        var state = Games.InPhase(phase, "Zoé");

        // When
        var transition = Games.Engine.Handle(state, Games.Select(Games.PackId), Games.Context());

        // Then
        Assert.Same(state, transition.State);
        Assert.Empty(transition.Effects);
        Assert.Equal(RejectionReason.GameAlreadyStarted, transition.Rejection);
    }

    [Fact]
    public void Handle_PacksLoadedWithTheSelectedPackStillValid_ReplacesTheCatalogAndKeepsTheSelection()
    {
        // Given
        var state = Games.LobbyWith("Zoé");
        var loaded = Games.Loaded(Games.Pack, _other, Games.InvalidPack("casse"));

        // When
        var transition = Games.Engine.Handle(state, loaded, Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        Assert.Empty(transition.Effects);
        Assert.Equal(state with { Catalog = loaded.Catalog }, transition.State);
    }

    [Fact]
    public void Handle_PacksLoadedWithTheSelectedPackNowInvalid_CancelsTheSelection()
    {
        // Given: the game master broke the selected pack on the disk
        var state = LobbyWith(Games.Pack, _other);

        // When
        var transition = Games.Engine.Handle(state, Games.Loaded(Games.InvalidPack(Games.PackId), _other), Games.Context());

        // Then: never replaced by the other pack, which the game master did not choose
        Assert.Null(transition.Rejection);
        Assert.Null(transition.State.SelectedPackId);
    }

    [Fact]
    public void Handle_PacksLoadedWithoutTheSelectedPack_CancelsTheSelection()
    {
        // Given: the game master removed the folder of the selected pack
        var state = LobbyWith(Games.Pack, _other);

        // When
        var transition = Games.Engine.Handle(state, Games.Loaded(_other), Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        Assert.Null(transition.State.SelectedPackId);
        Assert.Equal([_other], transition.State.Catalog.Packs);
    }

    [Fact]
    public void Handle_PacksLoadedWithoutSelectionAndASingleValidPack_ChoosesIt()
    {
        // Given: the game master fixed the only pack
        var state = Games.Accepted(Games.LobbyWith("Zoé"), Games.Loaded(Games.InvalidPack(Games.PackId)));

        // When
        var transition = Games.Engine.Handle(state, Games.Loaded(Games.Pack), Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        Assert.Equal(Games.PackId, transition.State.SelectedPackId);
    }

    [Fact]
    public void Handle_PacksLoadedWithoutSelectionAndSeveralValidPacks_ChoosesNone()
    {
        // Given
        var state = Games.Accepted(Games.LobbyWith("Zoé"), Games.Loaded());

        // When
        var transition = Games.Engine.Handle(state, Games.Loaded(Games.Pack, _other), Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        Assert.Null(transition.State.SelectedPackId);
    }

    [Fact]
    public void Handle_PacksLoadedEmpty_LeavesNothingToChoose()
    {
        // Given
        var state = Games.LobbyWith("Zoé");

        // When
        var transition = Games.Engine.Handle(state, Games.Loaded(), Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        Assert.Empty(transition.State.Catalog.Packs);
        Assert.Null(transition.State.SelectedPackId);
    }

    [Theory]
    [MemberData(nameof(Started))]
    public void Handle_PacksLoadedOnceStarted_IsRejectedAndTheGameKeepsItsPack(GamePhase phase)
    {
        // Given: a file of the pack changed on the disk during the game
        var state = Games.InPhase(phase, "Zoé");

        // When
        var transition = Games.Engine.Handle(state, Games.Loaded(Games.InvalidPack(Games.PackId)), Games.Context());

        // Then
        Assert.Same(state, transition.State);
        Assert.Empty(transition.Effects);
        Assert.Equal(RejectionReason.GameAlreadyStarted, transition.Rejection);
    }

    /// <summary>A lobby with a player, whose catalog holds <paramref name="packs"/>, still selecting <see cref="Games.Pack"/>.</summary>
    private static GameState LobbyWith(params CatalogPack[] packs) =>
        Games.Accepted(Games.LobbyWith("Zoé"), Games.Loaded(packs));
}
