using System.Text.Json;
using PartyGame.Contracts;
using PartyGame.Engine.Inputs;
using PartyGame.Engine.Modes;
using PartyGame.Engine.Modes.Common;
using PartyGame.Engine.Packs;
using PartyGame.Engine.State;
using PartyGame.Engine.Tests.Rounds;

namespace PartyGame.Engine.Tests.Lobby;

public sealed class PackPreviewingTests
{
    private static StartPreview StartPreview(string packId = "illustre") => new(packId, Games.Now);

    private static ShowPreviewStep Show(int round, int step, bool playExcerpt = false) => new(round, step, playExcerpt, Games.Now);

    /// <summary>A lobby where <see cref="Games.Pack"/> is chosen.</summary>
    private static GameState Lobby() => Games.Accepted(Games.LobbyWith("Zoé", "Max"), Games.Loaded(Games.Pack, Games.IllustratedPack()));

    /// <summary>The lobby of <see cref="Lobby"/>, where <see cref="Games.IllustratedPack"/> is previewed.</summary>
    private static GameState Previewed() => Games.Accepted(Lobby(), StartPreview());

    [Fact]
    public void Handle_StartPreviewInTheLobby_ShowsTheFirstStepOfTheFirstRound()
    {
        // When
        var transition = Games.Engine.Handle(Lobby(), StartPreview(), Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        Assert.Empty(transition.Effects);
        var state = transition.State;
        Assert.Equal(GamePhase.Lobby, state.Phase);
        Assert.Equal(Games.PackId, state.SelectedPackId);
        var preview = Games.Snapshots.ForDisplay(state).Preview;
        Assert.NotNull(preview);
        Assert.Equal((1, 2, "Échauffement"), (preview.Round.Number, preview.Round.Count, preview.Round.Title));
        Assert.Equal(new RoundStep(1, FakeMode.PreviewStepCount), preview.Step);
        Assert.Equal(state.Preview!.Media.UrlOf(Games.Flag), Assert.IsType<FakeDisplayView>(preview.View).ImageUrl);
        Assert.Equal(new GameMasterPreview("illustre", preview.Round, preview.Step, HasExcerpt: false), Games.Snapshots.ForGameMaster(state).Preview);
    }

    [Fact]
    public void Handle_StartPreview_DrawsMediaOfItsOwnAndLeavesTheGameWithoutAny()
    {
        // When
        var state = Previewed();

        // Then: the game itself has no media file until it starts.
        Assert.Empty(state.Media.Files);
        Assert.Equal([Games.Flag, Games.Monument], state.Preview!.Media.Files.Values.OrderBy(m => m.Value, StringComparer.Ordinal));
    }

    [Fact]
    public void Handle_StartPreview_ChangesNothingOnThePhones()
    {
        // Given
        var lobby = Lobby();

        // When
        var state = Games.Accepted(lobby, StartPreview());

        // Then
        Assert.All(lobby.Players, player => Assert.Equal(Games.Snapshots.ForPlayer(lobby, player), Games.Snapshots.ForPlayer(state, player)));
    }

    [Fact]
    public void Handle_StartPreviewOnceStarted_IsRejected()
    {
        // Given
        var state = Games.InPhase(GamePhase.Round, "Zoé");

        // When
        var transition = Games.Engine.Handle(state, StartPreview(Games.PackId), Games.Context());

        // Then
        Assert.Equal(RejectionReason.GameAlreadyStarted, transition.Rejection);
        Assert.Same(state, transition.State);
    }

    [Theory]
    [InlineData("inconnu", RejectionReason.PackUnknown)]
    [InlineData("pack-casse", RejectionReason.PackInvalid)]
    public void Handle_StartPreviewOfAPackThatCannotBePlayed_IsRejected(string packId, RejectionReason reason)
    {
        // Given
        var state = Games.Accepted(Games.LobbyWith("Zoé"), Games.Loaded(Games.Pack, Games.InvalidPack("pack-casse")));

        // When
        var transition = Games.Engine.Handle(state, StartPreview(packId), Games.Context());

        // Then
        Assert.Equal(reason, transition.Rejection);
        Assert.Same(state, transition.State);
    }

    [Fact]
    public void Handle_StartPreviewOfAPackWithoutItsGameMode_IsRejected()
    {
        // Given: a server without the mode of the pack
        var engine = new GameEngine(new GameModes([]));
        var state = Lobby();

        // When
        var transition = engine.Handle(state, StartPreview(), Games.Context());

        // Then
        Assert.Equal(RejectionReason.GameModeMissing, transition.Rejection);
    }

    [Fact]
    public void Handle_StartAnotherPreview_ShowsTheOtherPackFromItsStart()
    {
        // Given
        var state = Games.Accepted(Previewed(), Show(round: 2, step: 2));

        // When
        var other = Games.Accepted(state, StartPreview(Games.PackId));

        // Then
        Assert.Equal((Games.PackId, 0, 0), (other.Preview!.PackId, other.Preview.RoundIndex, other.Preview.StepIndex));
    }

    [Fact]
    public void Handle_ShowPreviewStep_ShowsThatStepOfThatRound()
    {
        // When
        var state = Games.Accepted(Previewed(), Show(round: 2, step: 3));

        // Then
        var preview = Games.Snapshots.ForDisplay(state).Preview!;
        Assert.Equal((2, "Finale"), (preview.Round.Number, preview.Round.Title));
        Assert.Equal(3, preview.Step.Number);
        Assert.Equal(state.Preview!.Media.UrlOf(Games.Monument), Assert.IsType<FakeDisplayView>(preview.View).ImageUrl);
    }

    [Fact]
    public void Handle_ShowTheStepShown_IsAcceptedWithoutChange()
    {
        // Given: a double tap, or a second console
        var state = Games.Accepted(Previewed(), Show(round: 2, step: 1));

        // When
        var transition = Games.Engine.Handle(state, Show(round: 2, step: 1), Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        Assert.Same(state, transition.State);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(3, 1)]
    [InlineData(1, 0)]
    [InlineData(1, FakeMode.PreviewStepCount + 1)]
    public void Handle_ShowAStepThePackHasNot_IsRejected(int round, int step)
    {
        // Given
        var state = Previewed();

        // When
        var transition = Games.Engine.Handle(state, Show(round, step), Games.Context());

        // Then
        Assert.Equal(RejectionReason.PreviewStepUnknown, transition.Rejection);
        Assert.Same(state, transition.State);
    }

    [Fact]
    public void Handle_ShowPreviewStepWithoutPreview_IsRejected()
    {
        // Given
        var state = Lobby();

        // When
        var transition = Games.Engine.Handle(state, Show(round: 1, step: 1), Games.Context());

        // Then
        Assert.Equal(RejectionReason.PreviewNotStarted, transition.Rejection);
        Assert.Same(state, transition.State);
    }

    [Fact]
    public void Handle_PlayTheExcerptOfAStep_PlaysItOnceTheTvScreenHadTimeToSetIt()
    {
        // When
        var state = Games.Accepted(Previewed(), Show(round: 1, step: 2, playExcerpt: true));

        // Then
        Assert.Equal(Games.Now + ExcerptPlayback.Lead, state.Preview!.ExcerptStartsAt);
        Assert.True(Games.Snapshots.ForGameMaster(state).Preview!.HasExcerpt);
    }

    [Fact]
    public void Handle_PlayTheExcerptAgain_PlaysItAgainFromItsStart()
    {
        // Given
        var state = Games.Accepted(Previewed(), Show(round: 1, step: 2, playExcerpt: true));
        var later = new GameContext(Games.Now.AddSeconds(8), new Random(42));

        // When
        var transition = Games.Engine.Handle(state, Show(round: 1, step: 2, playExcerpt: true), later);

        // Then
        Assert.Equal(later.Now + ExcerptPlayback.Lead, transition.State.Preview!.ExcerptStartsAt);
    }

    [Fact]
    public void Handle_ShowAnotherStep_StopsTheExcerpt()
    {
        // Given
        var state = Games.Accepted(Previewed(), Show(round: 1, step: 2, playExcerpt: true));

        // When
        var shown = Games.Accepted(state, Show(round: 1, step: 3));

        // Then
        Assert.Null(shown.Preview!.ExcerptStartsAt);
    }

    [Fact]
    public void Handle_StopPreview_GoesBackToTheLobby()
    {
        // Given
        var lobby = Lobby();
        var state = Games.Accepted(lobby, StartPreview());

        // When
        var stopped = Games.Accepted(state, new StopPreview(Games.Now));

        // Then
        Assert.Equal(lobby, stopped);
        Assert.Null(Games.Snapshots.ForDisplay(stopped).Preview);
        Assert.Null(Games.Snapshots.ForGameMaster(stopped).Preview);
    }

    [Fact]
    public void Handle_StopPreviewWithoutPreview_IsRejected()
    {
        // Given: sent twice, or by a second console
        var state = Lobby();

        // When
        var transition = Games.Engine.Handle(state, new StopPreview(Games.Now), Games.Context());

        // Then
        Assert.Equal(RejectionReason.PreviewNotStarted, transition.Rejection);
        Assert.Same(state, transition.State);
    }

    [Fact]
    public void Handle_StartTheGameDuringAPreview_EndsThePreview()
    {
        // When
        var state = Games.Accepted(Previewed(), Games.Start());

        // Then
        Assert.Equal(GamePhase.RoundIntro, state.Phase);
        Assert.Null(state.Preview);
        Assert.Null(Games.Snapshots.ForDisplay(state).Preview);
    }

    [Fact]
    public void GameState_SavedDuringAPreview_ComesBackWithoutIt()
    {
        // Given
        var options = GameStateJson.CreateOptions(Games.Modes, FakeJson.Options);

        // When
        var restored = JsonSerializer.Deserialize<GameState>(JsonSerializer.Serialize(Previewed(), options), options)!;

        // Then: a preview is no game, never resumed.
        Assert.Null(restored.Preview);
    }

    [Fact]
    public void Locate_AMediaFileOfThePreview_NamesTheRoundAndTheStepShown()
    {
        // Given
        var state = Games.Accepted(Previewed(), Show(round: 2, step: 3));
        var url = state.Preview!.Media.UrlOf(Games.Monument);

        // When
        var location = new MediaLocator(Games.Modes).Locate(state, new MediaId(url[(PackMedia.UrlPrefix.Length + 1)..]));

        // Then
        Assert.NotNull(location);
        Assert.Equal(Games.Monument, location.Media);
        Assert.Equal(state.Preview.Round, location.Round);
        Assert.Equal(3, location.Step);
    }
}
