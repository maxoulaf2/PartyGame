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
/// the artist they give apart, and the music resumes for the others while something is left to find.
/// </summary>
/// <remarks>
/// The reveal (US-E15-04) is to come: for now, the game master moves on by skipping the track.
/// </remarks>
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
    /// The buzzer of the player, closed until the music starts, and what they found: never the excerpt, which plays on the
    /// TV screen alone.
    /// </summary>
    /// <inheritdoc />
    public override PlayerRoundView ProjectForPlayer(BlindTestRound round, GameState game, Player player)
    {
        ArgumentNullException.ThrowIfNull(round);
        ArgumentNullException.ThrowIfNull(player);

        var buzzer = round.Buzzer;
        var state =
            round.Phase == BlindTestPhase.Ready ? BuzzerButtonState.Closed
            : buzzer.Blocked.Contains(player.Id) ? BuzzerButtonState.Blocked
            : round.Phase == BlindTestPhase.Closed ? BuzzerButtonState.Closed
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
            round.ArtistFoundBy == player.Id);
    }

    /// <summary>
    /// The excerpt to play under its opaque URL, then who has the hand and who found what: nothing of the title nor of the
    /// artist.
    /// </summary>
    /// <inheritdoc />
    public override DisplayRoundView ProjectForDisplay(BlindTestRound round, GameState game)
    {
        ArgumentNullException.ThrowIfNull(round);
        ArgumentNullException.ThrowIfNull(game);

        var excerpt = round.Track.Excerpt;
        return new BlindTestDisplayView(
            round.TrackNumber,
            round.Descriptor.Tracks.Length,
            PhaseOf(round),
            round.Playback.Project(game.Media.UrlOf(excerpt.File), ExcerptPlayback.EndOf(excerpt)),
            WinnerOf(round, game),
            NicknameOf(game, round.TitleFoundBy),
            NicknameOf(game, round.ArtistFoundBy));
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
    /// otherwise the buzzer closes and the music stays paused.
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
            return new(judged with { Buzzer = judged.Buzzer.Close() }, []);
        }

        var playback = round.Playback.Play(context.Now);
        return new(judged with { Playback = playback, Buzzer = judged.Buzzer.Reopen(playback.StartsAt!.Value) }, []);
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
        return round.TrackIndex == round.Descriptor.Tracks.Length - 1
            ? new(round, effects) { IsFinished = true }
            : new(new BlindTestRound(round.Descriptor, round.TrackIndex + 1), effects);
    }

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

    private static BlindTestTrackPhase PhaseOf(BlindTestRound round) => round.Phase switch
    {
        BlindTestPhase.Ready => BlindTestTrackPhase.Ready,

        // The arbitration window stays invisible: nobody learns that somebody buzzed before the winner is designated.
        BlindTestPhase.Listening or BlindTestPhase.Arbitrating => BlindTestTrackPhase.Listening,
        BlindTestPhase.Answering => BlindTestTrackPhase.Answering,
        BlindTestPhase.Closed => BlindTestTrackPhase.Closed,
        _ => throw new InvalidOperationException($"Phase {round.Phase} has no projection."),
    };
}
