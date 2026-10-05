using PartyGame.Contracts;
using PartyGame.Contracts.BlindTest;
using PartyGame.Contracts.Buzzer;
using PartyGame.Contracts.Packs;
using PartyGame.Engine.Audio;
using PartyGame.Engine.Effects;
using PartyGame.Engine.Inputs;
using PartyGame.Engine.Modes.BlindTest;
using EngineBuzzer = PartyGame.Engine.Buzzers.Buzzer;

namespace PartyGame.Engine.Tests.Modes.BlindTest;

/// <summary>
/// A track of a blind test round: announced and preloaded, played by the game master, which opens the buzzer as the music
/// starts, then paused while the player who pressed first has the hand.
/// </summary>
public sealed class BlindTestModeTests
{
    private static readonly BlindTestMode _mode = BlindTestGames.Mode;

    private static readonly string[] _players = ["Zoé", "Max", "Léa"];

    private static readonly BlindTestRoundDescriptor _round =
        BlindTestGames.Round(BlindTestGames.Ode, BlindTestGames.Moon, BlindTestGames.Anthem);

    private static GameState NewGame => BlindTestGames.Started(_round, _players);

    [Fact]
    public void Validate_RoundWhoseTitlesEarnPoints_ReportsNothing() =>
        Assert.Empty(_mode.Validate(_round with { ArtistPoints = 0 }, "$.rounds[0]"));

    [Fact]
    public void Validate_RoundWhoseArtistsAloneEarnPoints_ReportsNothing() =>
        Assert.Empty(_mode.Validate(_round with { TitlePoints = 0 }, "$.rounds[0]"));

    [Theory]
    [InlineData(0, 0, "Beethoven")]
    [InlineData(0, 500, null)]
    public void Validate_RoundWhereNoTrackEarnsPoints_ReportsMissingPoints(int titlePoints, int artistPoints, string? artist)
    {
        // When
        var problems = _mode.Validate(
            _round with { TitlePoints = titlePoints, ArtistPoints = artistPoints, Tracks = [BlindTestGames.Ode with { Artist = artist }] },
            "$.rounds[1]");

        // Then
        var problem = Assert.Single(problems);
        Assert.Equal(PackProblemCode.BlindTestPointsMissing, problem.Code);
        Assert.Equal(PackDescriptor.FileName, problem.File);
        Assert.Equal("$.rounds[1]", problem.Path);
        Assert.Empty(problem.Parameters);
    }

    [Fact]
    public void Start_Round_AnnouncesTheFirstTrackStandingAtTheStartOfItsExcerpt()
    {
        // When
        var transition = _mode.Start(_round with { Tracks = [BlindTestGames.Moon] }, Games.NewLobby(), Games.Context());

        // Then
        Assert.False(transition.IsFinished);
        Assert.Empty(transition.Effects);
        var round = Assert.IsType<BlindTestRound>(transition.State);
        Assert.Equal((1, BlindTestPhase.Ready), (round.TrackNumber, round.Phase));
        Assert.Equal(new ExcerptPlayback(9.5, null), round.Playback);
        Assert.False(round.Buzzer.IsOpen);
    }

    [Fact]
    public void Handle_Play_StartsTheMusicAfterTheLeadAndOpensTheBuzzerAtTheSameInstant()
    {
        // Given
        var state = NewGame;

        // When
        var transition = BlindTestGames.Engine.Handle(state, BlindTestGames.Play(state), Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        var round = BlindTestGames.RoundOf(transition.State);
        var startsAt = Games.Now.AddMilliseconds(500);
        Assert.Equal(BlindTestPhase.Listening, round.Phase);
        Assert.Equal(new ExcerptPlayback(0, startsAt), round.Playback);
        Assert.Equal((1, startsAt), (round.Buzzer.Opening, round.Buzzer.OpenedAt));
    }

    [Fact]
    public void Handle_PlayAnotherTrack_IsRejectedAsObsolete()
    {
        // Given
        var state = NewGame;

        // When
        var transition = BlindTestGames.Engine.Handle(state, BlindTestGames.Play(state, trackNumber: 2), Games.Context());

        // Then
        Assert.Equal(RejectionReason.QuestionMismatch, transition.Rejection);
        Assert.Same(state, transition.State);
    }

    [Fact]
    public void Handle_PlaySentAgain_IsRejectedAsObsolete()
    {
        // Given
        var state = BlindTestGames.Played(NewGame);

        // When
        var transition = BlindTestGames.Engine.Handle(state, BlindTestGames.Play(state), BlindTestGames.At(Games.Now.AddSeconds(3)));

        // Then
        Assert.Equal(RejectionReason.PhaseMismatch, transition.Rejection);
        Assert.Same(state, transition.State);
    }

    [Fact]
    public void Handle_BuzzWhileTheMusicPlays_StartsTheArbitrationAndTheMusicGoesOn()
    {
        // When
        var state = BlindTestGames.Buzzed(NewGame, (2, 3000));

        // Then
        var round = BlindTestGames.RoundOf(state);
        Assert.Equal(BlindTestPhase.Arbitrating, round.Phase);
        Assert.True(round.Playback.IsPlaying);
    }

    [Fact]
    public void Handle_ArbitrationElapsed_GivesTheHandToTheEarliestPressAndPausesTheMusic()
    {
        // When: Max pressed 3 s into the excerpt, Zoé 100 ms earlier but received later
        var state = BlindTestGames.Played(NewGame);
        var startsAt = BlindTestGames.StartsAt(state);
        state = BlindTestGames.Accepted(state, BlindTestGames.Buzz(state, 2, startsAt.AddSeconds(3)), startsAt.AddSeconds(3));
        var zoe = BlindTestGames.Buzz(state, 1, startsAt.AddMilliseconds(2900)) with { ReceivedAt = startsAt.AddMilliseconds(3050) };
        state = BlindTestGames.Accepted(state, zoe, zoe.ReceivedAt);
        var elapsed = BlindTestGames.ArbitrationElapsed(state);
        state = BlindTestGames.Accepted(state, elapsed, elapsed.DueAt);

        // Then: the music stops where the window ended, 3.25 s into the excerpt
        var round = BlindTestGames.RoundOf(state);
        Assert.Equal(BlindTestPhase.Answering, round.Phase);
        Assert.Equal(Games.PlayerIdOf(1), round.Buzzer.Winner);
        Assert.Equal(new ExcerptPlayback(3.25, null), round.Playback);
    }

    [Fact]
    public void Handle_ArbitrationElapsedOnceTheExcerptEnded_PausesAtItsEnd()
    {
        // When: buzzed 25 s into an excerpt of 15 s that starts at 9.5 s
        var state = BlindTestGames.Answering(BlindTestGames.AtTrack(NewGame, 1), (3, 25_000));

        // Then
        Assert.Equal(new ExcerptPlayback(24.5, null), BlindTestGames.RoundOf(state).Playback);
    }

    [Fact]
    public void Handle_BuzzBeforeTheExcerptIsPlayed_IsRejected()
    {
        // Given
        var state = NewGame;

        // When
        var transition = BlindTestGames.Engine.Handle(state, BlindTestGames.Buzz(state, 1, Games.Now), Games.Context());

        // Then
        Assert.Equal(RejectionReason.BuzzerClosed, transition.Rejection);
    }

    [Fact]
    public void Handle_BuzzOnAnotherTrack_IsRejectedAsObsolete()
    {
        // Given
        var state = BlindTestGames.Played(NewGame);

        // When
        var transition = BlindTestGames.Engine.Handle(state, BlindTestGames.Buzz(state, 1, BlindTestGames.StartsAt(state), trackNumber: 2), Games.Context());

        // Then
        Assert.Equal(RejectionReason.QuestionMismatch, transition.Rejection);
    }

    [Fact]
    public void Handle_BuzzWhileAPlayerHasTheHand_IsRejected()
    {
        // Given
        var state = BlindTestGames.Answering(NewGame, (1, 1000));

        // When
        var transition = BlindTestGames.Engine.Handle(state, BlindTestGames.Buzz(state, 2, Games.Now.AddSeconds(5)), Games.Context());

        // Then
        Assert.Equal(RejectionReason.BuzzerClosed, transition.Rejection);
    }

    [Fact]
    public void Handle_SkipTrack_AnnouncesTheNextTrackStandingAtItsStart()
    {
        // Given
        var state = BlindTestGames.Answering(NewGame, (1, 1000));

        // When
        var transition = BlindTestGames.Engine.Handle(state, BlindTestGames.SkipTrack(state), Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        Assert.Empty(transition.Effects);
        var round = BlindTestGames.RoundOf(transition.State);
        Assert.Equal((2, BlindTestPhase.Ready), (round.TrackNumber, round.Phase));
        Assert.Equal(new ExcerptPlayback(9.5, null), round.Playback);
        Assert.Empty(transition.State.Players.Where(player => player.Score != 0));
    }

    [Fact]
    public void Handle_SkipTrackDuringTheArbitration_CancelsItsTimer()
    {
        // Given
        var state = BlindTestGames.Buzzed(NewGame, (1, 1000));

        // When
        var transition = BlindTestGames.Engine.Handle(state, BlindTestGames.SkipTrack(state), Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        Assert.Equal(new CancelTimer(EngineBuzzer.ArbitrationTimer), Assert.Single(transition.Effects));
    }

    [Fact]
    public void Handle_SkipLastTrack_FinishesTheRound()
    {
        // Given
        var state = BlindTestGames.AtTrack(NewGame, 2);

        // When
        var transition = BlindTestGames.Engine.Handle(state, BlindTestGames.SkipTrack(state), Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        Assert.Equal(GamePhase.Finished, transition.State.Phase); // the only round of the pack
    }

    [Fact]
    public void Handle_SkipAnotherTrack_IsRejectedAsObsolete()
    {
        // Given: the first track skipped already, the request sent again
        var state = NewGame;
        var skip = BlindTestGames.SkipTrack(state);
        state = BlindTestGames.Accepted(state, skip);

        // When
        var transition = BlindTestGames.Engine.Handle(state, skip, Games.Context());

        // Then
        Assert.Equal(RejectionReason.QuestionMismatch, transition.Rejection);
        Assert.Equal(2, BlindTestGames.RoundOf(transition.State).TrackNumber);
    }

    [Fact]
    public void Handle_GameResumedWhileTheMusicPlays_GoesOnFromWhereItWasSaved()
    {
        // Given: saved 3 s into the excerpt, resumed half an hour later
        var state = BlindTestGames.Played(NewGame);
        var startsAt = BlindTestGames.StartsAt(state);
        var savedAt = startsAt.AddSeconds(3);
        var resumedAt = savedAt.AddMinutes(30);

        // When
        var transition = BlindTestGames.Engine.Handle(state, new GameResumed(savedAt), BlindTestGames.At(resumedAt));

        // Then: 3 s into the excerpt at the resumption
        Assert.Null(transition.Rejection);
        var round = BlindTestGames.RoundOf(transition.State);
        Assert.Equal(new ExcerptPlayback(0, resumedAt.AddSeconds(-3)), round.Playback);
        Assert.Equal(startsAt.AddMinutes(30), round.Buzzer.OpenedAt);
        Assert.Empty(transition.Effects);
    }

    [Fact]
    public void Handle_GameResumedDuringTheArbitration_GoesOnWithTheTimeLeft()
    {
        // Given: saved 100 ms into the window, resumed half an hour later
        var state = BlindTestGames.Buzzed(NewGame, (1, 1000));
        var savedAt = BlindTestGames.StartsAt(state).AddMilliseconds(1100);
        var resumedAt = savedAt.AddMinutes(30);

        // When
        var transition = BlindTestGames.Engine.Handle(state, new GameResumed(savedAt), BlindTestGames.At(resumedAt));

        // Then
        var arbitrateAt = resumedAt.AddMilliseconds(150);
        Assert.Equal(
            new ScheduleTimer(EngineBuzzer.ArbitrationTimer, arbitrateAt) { RoundId = state.CurrentRound!.Id },
            Assert.Single(transition.Effects));
    }

    [Fact]
    public void Handle_GameResumedWhileAPlayerHasTheHand_StaysPaused()
    {
        // Given
        var state = BlindTestGames.Answering(NewGame, (1, 1000));

        // When
        var transition = BlindTestGames.Engine.Handle(state, new GameResumed(Games.Now), BlindTestGames.At(Games.Now.AddMinutes(5)));

        // Then
        Assert.Equal(BlindTestGames.RoundOf(state).Playback, BlindTestGames.RoundOf(transition.State).Playback);
        Assert.Empty(transition.Effects);
    }

    [Fact]
    public void Handle_InputOfAnotherMode_IsRejected()
    {
        // Given
        var state = NewGame;
        var input = new GameMasterRoundInput(new BuzzerRevealAnswer(state.CurrentRound!.Id, 1), Games.Now);

        // When
        var transition = BlindTestGames.Engine.Handle(state, input, Games.Context());

        // Then
        Assert.Equal(RejectionReason.IntentUnsupported, transition.Rejection);
    }

    [Fact]
    public void ProjectForDisplay_MusicPlaying_DescribesThePlaybackUnderAnOpaqueUrl()
    {
        // Given
        var state = BlindTestGames.Played(BlindTestGames.AtTrack(NewGame, 1));

        // When
        var view = Assert.IsType<BlindTestDisplayView>(BlindTestGames.Snapshots.ForDisplay(state).RoundView);

        // Then
        var url = state.Media.UrlOf(BlindTestGames.Moon.Excerpt.File);
        Assert.StartsWith("/media/", url, StringComparison.Ordinal);
        Assert.Equal(
            new AudioPlayback(url, 9.5, 24.5, Games.Now.AddMilliseconds(500).ToUnixTimeMilliseconds()),
            view.Playback);
        Assert.Equal((2, 3, BlindTestTrackPhase.Listening, (string?)null), (view.TrackNumber, view.TrackCount, view.Phase, view.Winner));
    }

    [Fact]
    public void ProjectForDisplay_PlayerHasTheHand_NamesThemAndPausesTheMusic()
    {
        // Given
        var state = BlindTestGames.Answering(NewGame, (2, 1000));

        // When
        var view = Assert.IsType<BlindTestDisplayView>(BlindTestGames.Snapshots.ForDisplay(state).RoundView);

        // Then
        Assert.Equal((BlindTestTrackPhase.Answering, "Max"), (view.Phase, view.Winner));
        Assert.Equal((1.25, (long?)null), (view.Playback.Position, view.Playback.StartsAt));
    }

    [Fact]
    public void ProjectForPlayer_EachPhase_ShowsTheBuzzerOfThePlayer()
    {
        // Given
        var ready = NewGame;
        var played = BlindTestGames.Played(ready);
        var buzzed = BlindTestGames.Buzzed(ready, (2, 1000));
        var answering = BlindTestGames.Answering(ready, (2, 1000));

        // Then
        Assert.Equal((BuzzerButtonState.Closed, (long?)null), StateOf(ready, 1));
        var opensAt = BlindTestGames.StartsAt(played).ToUnixTimeMilliseconds();
        Assert.Equal((BuzzerButtonState.Open, (long?)opensAt), StateOf(played, 1));
        Assert.Equal(BuzzerButtonState.Buzzed, StateOf(buzzed, 2).Buzzer);
        Assert.Equal(BuzzerButtonState.Open, StateOf(buzzed, 1).Buzzer);
        Assert.Equal(BuzzerButtonState.Won, StateOf(answering, 2).Buzzer);
        Assert.Equal(BuzzerButtonState.Lost, StateOf(answering, 1).Buzzer);
    }

    [Fact]
    public void ProjectForGameMaster_TrackAnnounced_ShowsItsTitleAndArtist()
    {
        // When
        var view = Assert.IsType<BlindTestGameMasterView>(BlindTestGames.Snapshots.ForGameMaster(NewGame).RoundView);

        // Then
        Assert.Equal(
            (1, 3, BlindTestTrackPhase.Ready, "L'Hymne à la joie", (string?)"Ludwig van Beethoven"),
            (view.TrackNumber, view.TrackCount, view.Phase, view.Title, view.Artist));
    }

    [Fact]
    public void StepOf_TrackInProgress_IsItsNumber() =>
        Assert.Equal(new RoundStep(2, 3), _mode.StepOf(BlindTestGames.RoundOf(BlindTestGames.AtTrack(NewGame, 1))));

    [Theory]
    [InlineData("sons/la-marseillaise.mp3", 3)]
    [InlineData("images/croissant.png", 2)]
    [InlineData("sons/inconnu.mp3", null)]
    public void LocateMedia_FileOfATrack_IsTheNumberOfTheTrack(string media, int? expected) =>
        Assert.Equal(expected, _mode.LocateMedia(BlindTestGames.RoundOf(NewGame), new MediaPath(media)));

    private static (BuzzerButtonState Buzzer, long? OpensAt) StateOf(GameState state, int player)
    {
        var view = Assert.IsType<BlindTestPlayerView>(BlindTestGames.Snapshots.ForPlayer(state, state.Players[player - 1]).RoundView);
        return (view.Buzzer, view.OpensAt);
    }
}
