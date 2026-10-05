using PartyGame.Contracts;
using PartyGame.Contracts.BlindTest;
using PartyGame.Contracts.Buzzer;
using PartyGame.Contracts.Packs;
using PartyGame.Engine.Audio;
using PartyGame.Engine.Modes.BlindTest;

namespace PartyGame.Engine.Tests.Modes.BlindTest;

/// <summary>
/// The answer of the player who has the hand, judged by the game master on the title and the artist apart: each element
/// found is theirs, they may not buzz again on the track, and the music resumes for the others while something is left to
/// find.
/// </summary>
public sealed class BlindTestJudgementTests
{
    private static readonly string[] _players = ["Zoé", "Max", "Léa"];

    private static readonly BlindTestRoundDescriptor _round =
        BlindTestGames.Round(BlindTestGames.Ode, BlindTestGames.Moon, BlindTestGames.Anthem);

    private static GameState NewGame => BlindTestGames.Started(_round, _players);

    [Fact]
    public void Handle_TitleFound_AwardsItAndResumesTheMusicWhereItPaused()
    {
        // Given: Max has the hand, the music paused 1.25 s into the excerpt
        var state = BlindTestGames.Answering(NewGame, (2, 1000));
        var judgedAt = Games.Now.AddSeconds(5);

        // When
        var transition = BlindTestGames.Engine.Handle(state, BlindTestGames.Judge(state, title: true), BlindTestGames.At(judgedAt));

        // Then: the music goes on from 1.25 s, and the buzzer opens anew as it does
        Assert.Null(transition.Rejection);
        Assert.Empty(transition.Effects);
        var round = BlindTestGames.RoundOf(transition.State);
        var startsAt = judgedAt.AddMilliseconds(500);
        Assert.Equal((BlindTestPhase.Listening, Games.PlayerIdOf(2), (PlayerId?)null), (round.Phase, round.TitleFoundBy, round.ArtistFoundBy));
        Assert.Equal(new ExcerptPlayback(1.25, startsAt), round.Playback);
        Assert.Equal((2, startsAt), (round.Buzzer.Opening, round.Buzzer.OpenedAt));
        Assert.Equal([Games.PlayerIdOf(2)], round.Buzzer.Blocked);
        Assert.All(transition.State.Players, player => Assert.Equal(0, player.Score)); // points come with the reveal
    }

    [Fact]
    public void Handle_ArtistFound_AwardsItAndResumesTheMusic()
    {
        // When
        var state = BlindTestGames.Judged(NewGame, 2, artist: true);

        // Then
        var round = BlindTestGames.RoundOf(state);
        Assert.Equal((BlindTestPhase.Listening, (PlayerId?)null, Games.PlayerIdOf(2)), (round.Phase, round.TitleFoundBy, round.ArtistFoundBy));
    }

    [Fact]
    public void Handle_NothingFound_BlocksThePlayerAndResumesTheMusic()
    {
        // When
        var state = BlindTestGames.Judged(NewGame, 2);

        // Then: no points lost
        var round = BlindTestGames.RoundOf(state);
        Assert.Equal((BlindTestPhase.Listening, 2), (round.Phase, round.Buzzer.Opening));
        Assert.Equal((null, null), (round.TitleFoundBy, round.ArtistFoundBy));
        Assert.Equal([Games.PlayerIdOf(2)], round.Buzzer.Blocked);
        Assert.True(round.Playback.IsPlaying);
    }

    [Fact]
    public void Handle_BothFound_ClosesTheBuzzerAndKeepsTheMusicPaused()
    {
        // When
        var state = BlindTestGames.Judged(NewGame, 2, title: true, artist: true);

        // Then
        var round = BlindTestGames.RoundOf(state);
        Assert.Equal((BlindTestPhase.Closed, Games.PlayerIdOf(2), Games.PlayerIdOf(2)), (round.Phase, round.TitleFoundBy, round.ArtistFoundBy));
        Assert.Equal(new ExcerptPlayback(1.25, null), round.Playback);
        Assert.False(round.Buzzer.IsOpen);
    }

    [Fact]
    public void Handle_TitleOfATrackWithoutArtistFound_ClosesTheBuzzer()
    {
        // When
        var state = BlindTestGames.Judged(BlindTestGames.AtTrack(NewGame, 1), 3, title: true);

        // Then
        Assert.Equal(BlindTestPhase.Closed, BlindTestGames.RoundOf(state).Phase);
    }

    [Fact]
    public void Handle_TitleThenArtistFoundByAnotherPlayer_AwardsEachToItsFinder()
    {
        // Given: Max found the title, Léa has the hand on the music resumed
        var state = BlindTestGames.AnsweringAgain(BlindTestGames.Judged(NewGame, 2, title: true), 3);

        // When
        var transition = BlindTestGames.Engine.Handle(state, BlindTestGames.Judge(state, artist: true), Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        var round = BlindTestGames.RoundOf(transition.State);
        Assert.Equal((BlindTestPhase.Closed, Games.PlayerIdOf(2), Games.PlayerIdOf(3)), (round.Phase, round.TitleFoundBy, round.ArtistFoundBy));
    }

    [Fact]
    public void Handle_PlayerWhoFoundTheTitleBuzzesAgain_IsRejected()
    {
        // Given
        var state = BlindTestGames.Judged(NewGame, 2, title: true);

        // When
        var transition = BlindTestGames.Engine.Handle(state, BlindTestGames.Buzz(state, 2, BlindTestGames.StartsAt(state)), Games.Context());

        // Then
        Assert.Equal(RejectionReason.PlayerBlocked, transition.Rejection);
    }

    [Fact]
    public void Handle_LastConnectedPlayerJudged_ClosesTheBuzzer()
    {
        // Given: Léa left, Zoé found nothing, Max has the hand
        var state = BlindTestGames.AnsweringAgain(BlindTestGames.Judged(Disconnected(NewGame, "Léa"), 1), 2);

        // When
        var transition = BlindTestGames.Engine.Handle(state, BlindTestGames.Judge(state), Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        var round = BlindTestGames.RoundOf(transition.State);
        Assert.Equal(BlindTestPhase.Closed, round.Phase);
        Assert.False(round.Playback.IsPlaying);
    }

    [Fact]
    public void Handle_JudgeWhileNobodyHasTheHand_IsRejected()
    {
        // Given
        var state = BlindTestGames.Buzzed(NewGame, (1, 1000));

        // When
        var transition = BlindTestGames.Engine.Handle(state, BlindTestGames.Judge(state, title: true), Games.Context());

        // Then
        Assert.Equal(RejectionReason.PhaseMismatch, transition.Rejection);
        Assert.Same(state, transition.State);
    }

    [Fact]
    public void Handle_JudgeSentTwice_IsRejectedAsObsolete()
    {
        // Given: judged, the music resumed, and Léa has the hand now
        var answering = BlindTestGames.Answering(NewGame, (2, 1000));
        var judge = BlindTestGames.Judge(answering);
        var state = BlindTestGames.AnsweringAgain(BlindTestGames.Accepted(answering, judge), 3);

        // When
        var transition = BlindTestGames.Engine.Handle(state, judge, Games.Context());

        // Then
        Assert.Equal(RejectionReason.BuzzerOpeningMismatch, transition.Rejection);
        Assert.Same(state, transition.State);
    }

    [Fact]
    public void Handle_JudgeAnotherTrack_IsRejectedAsObsolete()
    {
        // Given
        var state = BlindTestGames.Answering(NewGame, (2, 1000));

        // When
        var transition = BlindTestGames.Engine.Handle(state, BlindTestGames.Judge(state, title: true, trackNumber: 2), Games.Context());

        // Then
        Assert.Equal(RejectionReason.QuestionMismatch, transition.Rejection);
    }

    [Fact]
    public void Handle_TitleFoundAlready_IsRejected()
    {
        // Given: Max found the title, Léa has the hand
        var state = BlindTestGames.AnsweringAgain(BlindTestGames.Judged(NewGame, 2, title: true), 3);

        // When
        var transition = BlindTestGames.Engine.Handle(state, BlindTestGames.Judge(state, title: true, artist: true), Games.Context());

        // Then
        Assert.Equal(RejectionReason.ElementAlreadyFound, transition.Rejection);
        Assert.Same(state, transition.State);
    }

    [Fact]
    public void Handle_ArtistOfATrackWithoutArtist_IsRejected()
    {
        // Given
        var state = BlindTestGames.Answering(BlindTestGames.AtTrack(NewGame, 1), (2, 1000));

        // When
        var transition = BlindTestGames.Engine.Handle(state, BlindTestGames.Judge(state, artist: true), Games.Context());

        // Then
        Assert.Equal(RejectionReason.ArtistMissing, transition.Rejection);
    }

    [Fact]
    public void Handle_SkipTrackOnceClosed_AnnouncesTheNextTrackWithNothingFound()
    {
        // Given
        var state = BlindTestGames.Judged(NewGame, 2, title: true, artist: true);

        // When
        var transition = BlindTestGames.Engine.Handle(state, BlindTestGames.SkipTrack(state), Games.Context());

        // Then
        var round = BlindTestGames.RoundOf(transition.State);
        Assert.Equal((2, BlindTestPhase.Ready), (round.TrackNumber, round.Phase));
        Assert.Equal((null, null), (round.TitleFoundBy, round.ArtistFoundBy));
    }

    [Fact]
    public void ProjectForPlayer_AfterJudgments_ShowsEachPlayerWhatTheyFound()
    {
        // Given: Max found the title, then Léa the artist
        var state = BlindTestGames.AnsweringAgain(BlindTestGames.Judged(NewGame, 2, title: true), 3);
        state = BlindTestGames.Accepted(state, BlindTestGames.Judge(state, artist: true));

        // Then
        Assert.Equal((BuzzerButtonState.Closed, false, false), ViewOf(state, 1));
        Assert.Equal((BuzzerButtonState.Blocked, true, false), ViewOf(state, 2));
        Assert.Equal((BuzzerButtonState.Blocked, false, true), ViewOf(state, 3));
    }

    [Fact]
    public void ProjectForPlayer_MusicResumed_ShowsTheOthersTheBuzzerOpen() =>
        Assert.Equal((BuzzerButtonState.Open, false, false), ViewOf(BlindTestGames.Judged(NewGame, 2, title: true), 1));

    [Fact]
    public void ProjectForDisplay_TitleFound_NamesItsFinderOnly()
    {
        // Given
        var state = BlindTestGames.Judged(NewGame, 2, title: true);

        // When
        var view = Assert.IsType<BlindTestDisplayView>(BlindTestGames.Snapshots.ForDisplay(state).RoundView);

        // Then
        Assert.Equal((BlindTestTrackPhase.Listening, (string?)null, (string?)"Max", (string?)null), (view.Phase, view.Winner, view.TitleFoundBy, view.ArtistFoundBy));
    }

    [Fact]
    public void ProjectForGameMaster_PlayerHasTheHand_NamesTheOpeningToJudge()
    {
        // Given
        var state = BlindTestGames.AnsweringAgain(BlindTestGames.Judged(NewGame, 2, artist: true), 1);

        // When
        var view = Assert.IsType<BlindTestGameMasterView>(BlindTestGames.Snapshots.ForGameMaster(state).RoundView);

        // Then
        Assert.Equal(
            (BlindTestTrackPhase.Answering, 2, (string?)"Zoé", (string?)null, (string?)"Max"),
            (view.Phase, view.Opening, view.Winner, view.TitleFoundBy, view.ArtistFoundBy));
    }

    private static (BuzzerButtonState Buzzer, bool FoundTitle, bool FoundArtist) ViewOf(GameState state, int player)
    {
        var view = Assert.IsType<BlindTestPlayerView>(BlindTestGames.Snapshots.ForPlayer(state, state.Players[player - 1]).RoundView);
        return (view.Buzzer, view.FoundTitle, view.FoundArtist);
    }

    private static GameState Disconnected(GameState state, string nickname) =>
        state with { Players = [.. state.Players.Select(player => player.Nickname == nickname ? player with { IsConnected = false } : player)] };
}
