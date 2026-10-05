using PartyGame.Contracts.BlindTest;
using PartyGame.Contracts.Packs;
using PartyGame.Engine.Audio;
using PartyGame.Engine.Effects;
using PartyGame.Engine.Modes.BlindTest;
using EngineBuzzer = PartyGame.Engine.Buzzers.Buzzer;

namespace PartyGame.Engine.Tests.Modes.BlindTest;

/// <summary>
/// The reveal of a track, once nothing is left to find, nobody is left to buzz, or the game master chooses to: the music
/// stops, the TV screen shows the track and who found what, and the elements found earn their points.
/// </summary>
public sealed class BlindTestRevealTests
{
    private static readonly string[] _players = ["Zoé", "Max", "Léa"];

    private static readonly BlindTestRoundDescriptor _round =
        BlindTestGames.Round(BlindTestGames.Ode, BlindTestGames.Moon, BlindTestGames.Anthem) with { TitlePoints = 300, ArtistPoints = 200 };

    private static GameState NewGame => BlindTestGames.Started(_round, _players);

    [Fact]
    public void Handle_RevealOnceTheTitleFound_AwardsTheTitleOnly()
    {
        // Given
        var state = BlindTestGames.Judged(NewGame, 2, title: true);

        // When
        var transition = BlindTestGames.Engine.Handle(state, BlindTestGames.RevealAnswer(state), Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        Assert.Equal(BlindTestPhase.Revealed, BlindTestGames.RoundOf(transition.State).Phase);
        Assert.Equal([0, 300, 0], Scores(transition.State));
    }

    [Fact]
    public void Handle_RevealOnceTheArtistFound_AwardsTheArtistOnly()
    {
        // Given
        var state = BlindTestGames.Judged(NewGame, 3, artist: true);

        // When
        state = BlindTestGames.Accepted(state, BlindTestGames.RevealAnswer(state));

        // Then
        Assert.Equal([0, 0, 200], Scores(state));
    }

    [Fact]
    public void Handle_TitleAndArtistFoundByTwoPlayers_RevealsAndAwardsEach()
    {
        // Given: Max found the title, Léa has the hand
        var state = BlindTestGames.AnsweringAgain(BlindTestGames.Judged(NewGame, 2, title: true), 3);

        // When: nothing left to find
        state = BlindTestGames.Accepted(state, BlindTestGames.Judge(state, artist: true));

        // Then
        Assert.Equal(BlindTestPhase.Revealed, BlindTestGames.RoundOf(state).Phase);
        Assert.Equal([0, 300, 200], Scores(state));
    }

    [Fact]
    public void Handle_BothFoundByOnePlayer_AwardsThemBoth()
    {
        // When
        var state = BlindTestGames.Judged(NewGame, 1, title: true, artist: true);

        // Then
        Assert.Equal([500, 0, 0], Scores(state));
    }

    [Fact]
    public void Handle_TitleOfATrackWithoutArtistFound_RevealsAndAwardsTheTitle()
    {
        // When
        var state = BlindTestGames.Judged(BlindTestGames.AtTrack(NewGame, 1), 2, title: true);

        // Then
        Assert.Equal(BlindTestPhase.Revealed, BlindTestGames.RoundOf(state).Phase);
        Assert.Equal([0, 300, 0], Scores(state));
    }

    [Fact]
    public void Handle_SkipTrackOnceTheTitleFound_AwardsNothing()
    {
        // Given
        var state = BlindTestGames.Judged(NewGame, 2, title: true);

        // When
        state = BlindTestGames.Accepted(state, BlindTestGames.SkipTrack(state));

        // Then
        Assert.Equal([0, 0, 0], Scores(state));
    }

    [Fact]
    public void Handle_RevealWhileTheMusicPlays_StopsItWhereItIs()
    {
        // Given: played, revealed 3 s after the music started
        var state = BlindTestGames.Played(NewGame);
        var revealedAt = BlindTestGames.StartsAt(state).AddSeconds(3);

        // When
        var transition = BlindTestGames.Engine.Handle(state, BlindTestGames.RevealAnswer(state), BlindTestGames.At(revealedAt));

        // Then
        Assert.Null(transition.Rejection);
        Assert.Empty(transition.Effects);
        var round = BlindTestGames.RoundOf(transition.State);
        Assert.Equal(new ExcerptPlayback(3, null), round.Playback);
        Assert.Equal(BlindTestPhase.Revealed, round.Phase);
        Assert.Equal([0, 0, 0], Scores(transition.State));
    }

    [Fact]
    public void Handle_RevealDuringTheArbitration_CancelsItsTimer()
    {
        // Given
        var state = BlindTestGames.Buzzed(NewGame, (1, 1000));

        // When
        var transition = BlindTestGames.Engine.Handle(state, BlindTestGames.RevealAnswer(state), Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        Assert.Equal(new CancelTimer(EngineBuzzer.ArbitrationTimer), Assert.Single(transition.Effects));
        Assert.Null(BlindTestGames.RoundOf(transition.State).Buzzer.Winner);
    }

    [Fact]
    public void Handle_RevealWhileAPlayerHasTheHand_AwardsNothingToThem()
    {
        // Given
        var state = BlindTestGames.Answering(NewGame, (2, 1000));

        // When
        state = BlindTestGames.Accepted(state, BlindTestGames.RevealAnswer(state));

        // Then
        Assert.Equal(BlindTestPhase.Revealed, BlindTestGames.RoundOf(state).Phase);
        Assert.Equal([0, 0, 0], Scores(state));
    }

    [Fact]
    public void Handle_RevealBeforeTheExcerptIsPlayed_IsRejected()
    {
        // Given
        var state = NewGame;

        // When
        var transition = BlindTestGames.Engine.Handle(state, BlindTestGames.RevealAnswer(state), Games.Context());

        // Then
        Assert.Equal(RejectionReason.PhaseMismatch, transition.Rejection);
        Assert.Same(state, transition.State);
    }

    [Fact]
    public void Handle_RevealSentTwice_IsRejectedWithoutAwardingTwice()
    {
        // Given
        var state = BlindTestGames.Judged(NewGame, 2, title: true);
        state = BlindTestGames.Accepted(state, BlindTestGames.RevealAnswer(state));

        // When
        var transition = BlindTestGames.Engine.Handle(state, BlindTestGames.RevealAnswer(state), Games.Context());

        // Then
        Assert.Equal(RejectionReason.PhaseMismatch, transition.Rejection);
        Assert.Equal([0, 300, 0], Scores(transition.State));
    }

    [Fact]
    public void Handle_RevealAnotherTrack_IsRejectedAsObsolete()
    {
        // Given
        var state = BlindTestGames.Played(NewGame);

        // When
        var transition = BlindTestGames.Engine.Handle(state, BlindTestGames.RevealAnswer(state, trackNumber: 2), Games.Context());

        // Then
        Assert.Equal(RejectionReason.QuestionMismatch, transition.Rejection);
    }

    [Fact]
    public void Handle_NextTrackOnceRevealed_AnnouncesTheNextTrack()
    {
        // Given
        var state = BlindTestGames.Judged(NewGame, 2, title: true, artist: true);

        // When
        var transition = BlindTestGames.Engine.Handle(state, BlindTestGames.NextTrack(state), Games.Context());

        // Then: the points stay
        Assert.Null(transition.Rejection);
        var round = BlindTestGames.RoundOf(transition.State);
        Assert.Equal((2, BlindTestPhase.Ready), (round.TrackNumber, round.Phase));
        Assert.Equal([0, 500, 0], Scores(transition.State));
    }

    [Fact]
    public void Handle_NextTrackAfterTheLastTrack_EndsTheRound()
    {
        // Given
        var state = BlindTestGames.Judged(BlindTestGames.AtTrack(NewGame, 2), 2, title: true, artist: true);

        // When
        var transition = BlindTestGames.Engine.Handle(state, BlindTestGames.NextTrack(state), Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        Assert.Equal(GamePhase.Finished, transition.State.Phase); // the only round of the pack
    }

    [Fact]
    public void Handle_NextTrackBeforeTheReveal_IsRejected()
    {
        // Given
        var state = BlindTestGames.Played(NewGame);

        // When
        var transition = BlindTestGames.Engine.Handle(state, BlindTestGames.NextTrack(state), Games.Context());

        // Then
        Assert.Equal(RejectionReason.PhaseMismatch, transition.Rejection);
    }

    [Fact]
    public void Handle_NextTrackSentTwice_IsRejectedAsObsolete()
    {
        // Given: moved on already, the request sent again
        var state = BlindTestGames.Judged(NewGame, 2, title: true, artist: true);
        var next = BlindTestGames.NextTrack(state);
        state = BlindTestGames.Accepted(state, next);

        // When
        var transition = BlindTestGames.Engine.Handle(state, next, Games.Context());

        // Then
        Assert.Equal(RejectionReason.QuestionMismatch, transition.Rejection);
        Assert.Equal(2, BlindTestGames.RoundOf(transition.State).TrackNumber);
    }

    [Fact]
    public void ProjectForDisplay_Revealed_ShowsTheTrackAndWhoFoundWhat()
    {
        // Given: the track with an image, its title found by Max
        var state = BlindTestGames.Judged(BlindTestGames.AtTrack(NewGame, 1), 2, title: true);

        // When
        var view = Assert.IsType<BlindTestDisplayView>(BlindTestGames.Snapshots.ForDisplay(state).RoundView);

        // Then
        Assert.Equal(BlindTestTrackPhase.Revealed, view.Phase);
        Assert.Equal(("Au clair de la lune", (string?)null, (string?)"Max"), (view.Title, view.Artist, view.TitleFoundBy));
        Assert.Equal(state.Media.UrlOf(BlindTestGames.Moon.Image!.Value), view.ImageUrl);
        Assert.Null(view.Playback.StartsAt);
    }

    [Fact]
    public void ProjectForDisplay_BeforeTheReveal_ShowsNothingOfTheTrack()
    {
        // Given
        var state = BlindTestGames.Answering(BlindTestGames.AtTrack(NewGame, 1), (2, 1000));

        // When
        var view = Assert.IsType<BlindTestDisplayView>(BlindTestGames.Snapshots.ForDisplay(state).RoundView);

        // Then
        Assert.Equal((null, null, null), (view.Title, view.Artist, view.ImageUrl));
    }

    [Fact]
    public void ProjectForPlayer_Revealed_ShowsThePointsOfEachPlayer()
    {
        // Given: Max found the title, Léa the artist
        var state = BlindTestGames.AnsweringAgain(BlindTestGames.Judged(NewGame, 2, title: true), 3);
        var judged = BlindTestGames.Accepted(state, BlindTestGames.Judge(state, artist: true));

        // Then: nothing before the reveal
        Assert.Null(PointsOf(state, 2));
        Assert.Equal([0, 300, 200], [PointsOf(judged, 1), PointsOf(judged, 2), PointsOf(judged, 3)]);
    }

    private static int[] Scores(GameState state) => [.. state.Players.Select(player => player.Score)];

    private static int? PointsOf(GameState state, int player) =>
        Assert.IsType<BlindTestPlayerView>(BlindTestGames.Snapshots.ForPlayer(state, state.Players[player - 1]).RoundView).Points;
}
