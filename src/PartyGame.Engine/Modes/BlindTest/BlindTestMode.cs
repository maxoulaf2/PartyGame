using System.Collections.Immutable;
using PartyGame.Contracts;
using PartyGame.Contracts.BlindTest;
using PartyGame.Contracts.Buzzer;
using PartyGame.Contracts.Packs;
using PartyGame.Engine.Audio;
using PartyGame.Engine.Buzzers;
using PartyGame.Engine.Effects;
using PartyGame.Engine.Inputs;

namespace PartyGame.Engine.Modes.BlindTest;

/// <summary>
/// Plays the blind test rounds of the packs: the game master plays the excerpt of each track on the TV screen, which opens
/// the buzzer, and the music pauses once the first player who pressed has the hand. The game master judges the title and
/// the artist they give apart, and the music resumes for the others while something is left to find. The reveal shows the
/// track on the TV screen and awards the points of the elements found.
/// </summary>
public sealed class BlindTestMode : GameMode<BlindTestRoundDescriptor, BlindTestRound>
{
    /// <summary>
    /// Checks that at least one track earns points: otherwise the round could not tell the players apart.
    /// </summary>
    /// <inheritdoc />
    public override ImmutableArray<PackProblem> Validate(BlindTestRoundDescriptor descriptor, string path)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        var earnsPoints = descriptor.TitlePoints > 0
            || (descriptor.ArtistPoints > 0 && descriptor.Tracks.Any(track => track.Artist is not null));
        return earnsPoints
            ? []
            : [new PackProblem(PackProblemCode.BlindTestPointsMissing, PackDescriptor.FileName, path, ImmutableDictionary<string, string>.Empty)];
    }

    /// <summary>
    /// Announces the first track of the round: its excerpt preloaded on the TV screen, its buzzer closed.
    /// </summary>
    /// <inheritdoc />
    public override RoundTransition Start(BlindTestRoundDescriptor descriptor, GameState game, GameContext context) =>
        new(new BlindTestRound(descriptor, 0), []);

    /// <summary>
    /// Plays the intents of the round, each aimed at the track it names, and the end of the arbitration window.
    /// </summary>
    /// <inheritdoc />
    public override RoundTransition Handle(BlindTestRound round, GameInput input, GameState game, GameContext context)
    {
        ArgumentNullException.ThrowIfNull(round);
        ArgumentNullException.ThrowIfNull(context);

        return input switch
        {
            GameMasterRoundInput { RoundIntent: BlindTestPlay play } => Play(round, play, context),
            GameMasterRoundInput { RoundIntent: BlindTestJudge judge } => Judge(round, judge, game, context),
            GameMasterRoundInput { RoundIntent: BlindTestRevealAnswer reveal } => RevealAnswer(round, reveal, context),
            GameMasterRoundInput { RoundIntent: BlindTestNextTrack next } => NextTrack(round, next),
            GameMasterRoundInput { RoundIntent: BlindTestSkipTrack skip } => SkipTrack(round, skip),
            PlayerRoundInput { RoundIntent: BlindTestBuzz buzz } buzzed => Buzz(round, buzzed.PlayerId, buzz, buzzed.ReceivedAt, context),
            TimerElapsed timer => Arbitrate(round, timer, context),
            _ => RoundTransition.Rejected(round, RejectionReason.IntentUnsupported),
        };
    }

    /// <summary>
    /// Moves the times of the playback and of the buzzer on by the time spent offline: the music goes on from where it was
    /// at the last save, and an arbitration window in progress goes on with the time it had left.
    /// </summary>
    /// <inheritdoc />
    public override RoundTransition ResumeRound(BlindTestRound round, GameState game, TimeSpan shift, GameContext context)
    {
        ArgumentNullException.ThrowIfNull(round);
        var buzzer = round.Buzzer.Resume(shift);
        return new(round with { Playback = round.Playback.Resume(shift), Buzzer = buzzer.Buzzer }, buzzer.Effects);
    }

    /// <summary>
    /// The buzzer of the player, closed until the music starts, what they found, and the points they earned once revealed:
    /// never the excerpt, which plays on the TV screen alone.
    /// </summary>
    /// <inheritdoc />
    public override PlayerRoundView ProjectForPlayer(BlindTestRound round, GameState game, Player player)
    {
        ArgumentNullException.ThrowIfNull(round);
        ArgumentNullException.ThrowIfNull(player);

        var buzzer = round.Buzzer;
        var state =
            round.Phase is BlindTestPhase.Ready or BlindTestPhase.Revealed ? BuzzerButtonState.Closed
            : buzzer.Blocked.Contains(player.Id) ? BuzzerButtonState.Blocked
            : buzzer.Winner == player.Id ? BuzzerButtonState.Won
            : buzzer.Winner is not null ? BuzzerButtonState.Lost

            // Whether this player buzzed, never whether the others did: the arbitration is not over.
            : buzzer.Presses.Any(press => press.PlayerId == player.Id) ? BuzzerButtonState.Buzzed
            : BuzzerButtonState.Open;
        return new BlindTestPlayerView(
            round.TrackNumber,
            round.Descriptor.Tracks.Length,
            buzzer.Opening,
            buzzer.Opening > 0 ? buzzer.OpenedAt.ToUnixTimeMilliseconds() : null,
            state,
            WinnerOf(round, game),
            round.TitleFoundBy == player.Id,
            round.ArtistFoundBy == player.Id,
            round.Phase == BlindTestPhase.Revealed ? PointsOf(round, player.Id) : null);
    }

    /// <summary>
    /// The excerpt to play under its opaque URL, then who has the hand and who found what: nothing of the title, of the
    /// artist nor of the image before the reveal.
    /// </summary>
    /// <inheritdoc />
    public override DisplayRoundView ProjectForDisplay(BlindTestRound round, GameState game)
    {
        ArgumentNullException.ThrowIfNull(round);
        ArgumentNullException.ThrowIfNull(game);

        var excerpt = round.Track.Excerpt;
        var revealed = round.Phase == BlindTestPhase.Revealed;
        return new BlindTestDisplayView(
            round.TrackNumber,
            round.Descriptor.Tracks.Length,
            PhaseOf(round),
            round.Playback.Project(game.Media.UrlOf(excerpt.File), ExcerptPlayback.EndOf(excerpt)),
            WinnerOf(round, game),
            NicknameOf(game, round.TitleFoundBy),
            NicknameOf(game, round.ArtistFoundBy),
            revealed ? round.Track.Title : null,
            revealed ? round.Track.Artist : null,
            revealed && round.Track.Image is { } image ? game.Media.UrlOf(image) : null);
    }

    /// <inheritdoc />
    public override GameMasterRoundView ProjectForGameMaster(BlindTestRound round, GameState game)
    {
        ArgumentNullException.ThrowIfNull(round);
        return new BlindTestGameMasterView(
            round.TrackNumber,
            round.Descriptor.Tracks.Length,
            PhaseOf(round),
            round.Buzzer.Opening,
            round.Track.Title,
            round.Track.Artist,
            WinnerOf(round, game),
            NicknameOf(game, round.TitleFoundBy),
            NicknameOf(game, round.ArtistFoundBy));
    }

    /// <summary>
    /// The track in progress among those of the round.
    /// </summary>
    /// <inheritdoc />
    public override RoundStep? StepOf(BlindTestRound round)
    {
        ArgumentNullException.ThrowIfNull(round);
        return new RoundStep(round.TrackNumber, round.Descriptor.Tracks.Length);
    }

    /// <summary>
    /// One step per track.
    /// </summary>
    /// <inheritdoc />
    public override int CountPreviewSteps(BlindTestRoundDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        return descriptor.Tracks.Length;
    }

    /// <summary>
    /// The track revealed, found by nobody, its excerpt played from its start when asked.
    /// </summary>
    /// <inheritdoc />
    public override RoundPreview Preview(BlindTestRoundDescriptor descriptor, int stepIndex, DateTimeOffset? excerptStartsAt)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        var round = new BlindTestRound(descriptor, stepIndex)
        {
            // Opened once and closed: the phase of a track revealed.
            Buzzer = new Buzzers.Buzzer { Opening = 1, IsClosed = true },
        };
        return new(round with { Playback = round.Playback with { StartsAt = excerptStartsAt } }, HasExcerpt: true);
    }

    /// <summary>
    /// The number of the track whose excerpt or image the file is: the track in progress when it is, since the TV screen
    /// plays only its excerpt, or else the first one of the round.
    /// </summary>
    /// <inheritdoc />
    public override int? LocateMedia(BlindTestRound round, MediaPath media)
    {
        ArgumentNullException.ThrowIfNull(round);
        if (Uses(round.Track, media))
        {
            return round.TrackNumber;
        }

        var tracks = round.Descriptor.Tracks;
        for (var index = 0; index < tracks.Length; index++)
        {
            if (Uses(tracks[index], media))
            {
                return index + 1;
            }
        }

        return null;
    }

    private static bool Uses(BlindTestTrack track, MediaPath media) => track.Excerpt.File == media || track.Image == media;

    /// <summary>
    /// Plays the excerpt of the track announced, <see cref="ExcerptPlayback.Lead"/> from now, and opens its buzzer to
    /// every player at the same instant.
    /// </summary>
    private static RoundTransition Play(BlindTestRound round, BlindTestPlay play, GameContext context)
    {
        RejectionReason? rejection =
            play.TrackNumber != round.TrackNumber ? RejectionReason.QuestionMismatch
            : round.Phase != BlindTestPhase.Ready ? RejectionReason.PhaseMismatch
            : null;
        if (rejection is { } reason)
        {
            return RoundTransition.Rejected(round, reason);
        }

        var playback = round.Playback.Play(context.Now);
        return new(round with { Playback = playback, Buzzer = round.Buzzer.Open(playback.StartsAt!.Value) }, []);
    }

    /// <summary>
    /// Judges the answer of the player who has the hand: each element they found is theirs, and they may not buzz again on
    /// the track, whatever they found. While something is left to find and a connected player may buzz, the music resumes
    /// where it paused, <see cref="ExcerptPlayback.Lead"/> from now, and the buzzer opens anew at the same instant;
    /// otherwise the track is revealed.
    /// </summary>
    private static RoundTransition Judge(BlindTestRound round, BlindTestJudge judge, GameState game, GameContext context)
    {
        RejectionReason? rejection =
            judge.TrackNumber != round.TrackNumber ? RejectionReason.QuestionMismatch
            : round.Phase != BlindTestPhase.Answering ? RejectionReason.PhaseMismatch

            // Judged already, the buzzer reopened and somebody else has the hand: this judgment is not about them.
            : judge.Opening != round.Buzzer.Opening ? RejectionReason.BuzzerOpeningMismatch
            : judge.ArtistFound && round.Track.Artist is null ? RejectionReason.ArtistMissing
            : (judge.TitleFound && round.TitleFoundBy is not null) || (judge.ArtistFound && round.ArtistFoundBy is not null)
                ? RejectionReason.ElementAlreadyFound
            : null;
        if (rejection is { } reason)
        {
            return RoundTransition.Rejected(round, reason);
        }

        var winner = round.Buzzer.Winner!.Value;
        var judged = round with
        {
            TitleFoundBy = judge.TitleFound ? winner : round.TitleFoundBy,
            ArtistFoundBy = judge.ArtistFound ? winner : round.ArtistFoundBy,
            Buzzer = round.Buzzer.Block(winner),
        };
        var anybodyLeft = game.Players.Any(player => player.IsConnected && !judged.Buzzer.Blocked.Contains(player.Id));
        if (!judged.IsSomethingLeft || !anybodyLeft)
        {
            return Reveal(judged, context);
        }

        var playback = round.Playback.Play(context.Now);
        return new(judged with { Playback = playback, Buzzer = judged.Buzzer.Reopen(playback.StartsAt!.Value) }, []);
    }

    /// <summary>
    /// Reveals the track played, whoever has the hand: an arbitration window in progress is abandoned.
    /// </summary>
    private static RoundTransition RevealAnswer(BlindTestRound round, BlindTestRevealAnswer reveal, GameContext context)
    {
        RejectionReason? rejection =
            reveal.TrackNumber != round.TrackNumber ? RejectionReason.QuestionMismatch
            : round.Phase is BlindTestPhase.Ready or BlindTestPhase.Revealed ? RejectionReason.PhaseMismatch
            : null;
        if (rejection is { } reason)
        {
            return RoundTransition.Rejected(round, reason);
        }

        var transition = Reveal(round, context);
        return round.Phase == BlindTestPhase.Arbitrating
            ? transition with { Effects = [new CancelTimer(Buzzers.Buzzer.ArbitrationTimer)] }
            : transition;
    }

    /// <summary>
    /// Closes the buzzer, stops the music where it is, and awards the points of the elements found.
    /// </summary>
    private static RoundTransition Reveal(BlindTestRound round, GameContext context)
    {
        var revealed = round with
        {
            Buzzer = round.Buzzer.Close(),
            Playback = round.Playback.Pause(context.Now, ExcerptPlayback.EndOf(round.Track.Excerpt)),
        };
        var points = new[] { round.TitleFoundBy, round.ArtistFoundBy }
            .OfType<PlayerId>()
            .Distinct()
            .ToImmutableDictionary(player => player, player => PointsOf(round, player));
        return new(revealed, []) { Points = points };
    }

    /// <summary>
    /// Moves on from the revealed track: announces the next one, its buzzer closed, or ends the round after the last one.
    /// </summary>
    private static RoundTransition NextTrack(BlindTestRound round, BlindTestNextTrack next)
    {
        RejectionReason? rejection =
            next.TrackNumber != round.TrackNumber ? RejectionReason.QuestionMismatch
            : round.Phase != BlindTestPhase.Revealed ? RejectionReason.PhaseMismatch
            : null;
        return rejection is { } reason ? RoundTransition.Rejected(round, reason) : MoveOn(round, []);
    }

    /// <summary>
    /// Skips the track in progress without points, whatever its phase: announces the next one, or ends the round after the
    /// last one, the round then staying on it. An arbitration window in progress is abandoned.
    /// </summary>
    private static RoundTransition SkipTrack(BlindTestRound round, BlindTestSkipTrack skip)
    {
        if (skip.TrackNumber != round.TrackNumber)
        {
            return RoundTransition.Rejected(round, RejectionReason.QuestionMismatch);
        }

        ImmutableArray<Effect> effects = round.Phase == BlindTestPhase.Arbitrating ? [new CancelTimer(Buzzers.Buzzer.ArbitrationTimer)] : [];
        return MoveOn(round, effects);
    }

    /// <summary>
    /// Announces the next track, or ends the round after the last one, the round then staying on it.
    /// </summary>
    private static RoundTransition MoveOn(BlindTestRound round, ImmutableArray<Effect> effects) =>
        round.TrackIndex == round.Descriptor.Tracks.Length - 1
            ? new(round, effects) { IsFinished = true }
            : new(new BlindTestRound(round.Descriptor, round.TrackIndex + 1), effects);

    /// <summary>
    /// Hands a buzz on the track in progress to its buzzer, which judges it.
    /// </summary>
    private static RoundTransition Buzz(BlindTestRound round, PlayerId player, BlindTestBuzz buzz, DateTimeOffset receivedAt, GameContext context) =>
        buzz.TrackNumber != round.TrackNumber
            ? RoundTransition.Rejected(round, RejectionReason.QuestionMismatch)
            : Apply(round, round.Buzzer.Buzz(player, buzz.Opening, buzz.PressedAt, receivedAt, context));

    /// <summary>
    /// Designates the winner at the end of the arbitration window, and pauses the music where it is while they answer.
    /// </summary>
    private static RoundTransition Arbitrate(BlindTestRound round, TimerElapsed timer, GameContext context)
    {
        var transition = Apply(round, round.Buzzer.Arbitrate(timer));
        return transition.Rejection is not null
            ? transition
            : transition with
            {
                State = (BlindTestRound)transition.State with
                {
                    Playback = round.Playback.Pause(context.Now, ExcerptPlayback.EndOf(round.Track.Excerpt)),
                },
            };
    }

    /// <summary>
    /// The round with the buzzer a transition produced, or the very same round when the buzzer rejected the input.
    /// </summary>
    private static RoundTransition Apply(BlindTestRound round, BuzzerTransition transition) =>
        transition.Rejection is { } reason
            ? RoundTransition.Rejected(round, reason)
            : new(round with { Buzzer = transition.Buzzer }, transition.Effects);

    /// <summary>
    /// The nickname of the player who has the hand, public once designated.
    /// </summary>
    private static string? WinnerOf(BlindTestRound round, GameState game) => NicknameOf(game, round.Buzzer.Winner);

    /// <summary>
    /// Players are never removed: whoever buzzed is still registered, under their current nickname.
    /// </summary>
    private static string? NicknameOf(GameState game, PlayerId? id) =>
        id is { } player ? game.Players.First(p => p.Id == player).Nickname : null;

    /// <summary>
    /// What a player earned with the track: the points of each element they found.
    /// </summary>
    private static int PointsOf(BlindTestRound round, PlayerId player) =>
        (round.TitleFoundBy == player ? round.Descriptor.TitlePoints : 0)
        + (round.ArtistFoundBy == player ? round.Descriptor.ArtistPoints : 0);

    private static BlindTestTrackPhase PhaseOf(BlindTestRound round) => round.Phase switch
    {
        BlindTestPhase.Ready => BlindTestTrackPhase.Ready,

        // The arbitration window stays invisible: nobody learns that somebody buzzed before the winner is designated.
        BlindTestPhase.Listening or BlindTestPhase.Arbitrating => BlindTestTrackPhase.Listening,
        BlindTestPhase.Answering => BlindTestTrackPhase.Answering,
        BlindTestPhase.Revealed => BlindTestTrackPhase.Revealed,
        _ => throw new InvalidOperationException($"Phase {round.Phase} has no projection."),
    };
}
