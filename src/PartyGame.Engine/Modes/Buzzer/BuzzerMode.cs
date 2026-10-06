using System.Collections.Immutable;
using PartyGame.Contracts;
using PartyGame.Contracts.Buzzer;
using PartyGame.Contracts.Packs;
using PartyGame.Engine.Buzzers;
using PartyGame.Engine.Effects;
using PartyGame.Engine.Inputs;

namespace PartyGame.Engine.Modes.Buzzer;

/// <summary>
/// Plays the rounds of buzzer questions of the packs: the game master asks each question, which opens the buzzer, the
/// first player who pressed has the hand, and the game master judges their answer, until somebody finds it or the game
/// master reveals it.
/// </summary>
public sealed class BuzzerMode : GameMode<BuzzerRoundDescriptor, BuzzerRound>
{
    /// <summary>
    /// Reports nothing: the constraints of the descriptor cover every rule of a buzzer round.
    /// </summary>
    /// <inheritdoc />
    public override ImmutableArray<PackProblem> Validate(BuzzerRoundDescriptor descriptor, string path) => [];

    /// <summary>
    /// Announces the first question of the round, its buzzer closed.
    /// </summary>
    /// <inheritdoc />
    public override RoundTransition Start(BuzzerRoundDescriptor descriptor, GameState game, GameContext context) =>
        new(new BuzzerRound(descriptor, 0), []);

    /// <summary>
    /// Plays the intents of the round, each aimed at the question it names, and the end of the arbitration window.
    /// </summary>
    /// <inheritdoc />
    public override RoundTransition Handle(BuzzerRound round, GameInput input, GameState game, GameContext context)
    {
        ArgumentNullException.ThrowIfNull(round);
        ArgumentNullException.ThrowIfNull(context);

        return input switch
        {
            GameMasterRoundInput { RoundIntent: BuzzerAskQuestion ask } => AskQuestion(round, ask, context),
            GameMasterRoundInput { RoundIntent: BuzzerShowQuestion show } => ShowQuestion(round, show),
            GameMasterRoundInput { RoundIntent: BuzzerJudge judge } => Judge(round, judge, game, context),
            GameMasterRoundInput { RoundIntent: BuzzerRevealAnswer reveal } => RevealAnswer(round, reveal),
            GameMasterRoundInput { RoundIntent: BuzzerNextQuestion next } => NextQuestion(round, next),
            PlayerRoundInput { RoundIntent: BuzzerBuzz buzz } buzzed => Buzz(round, buzzed.PlayerId, buzz, buzzed.ReceivedAt, context),
            TimerElapsed timer => Apply(round, round.Buzzer.Arbitrate(timer)),
            _ => RoundTransition.Rejected(round, RejectionReason.IntentUnsupported),
        };
    }

    /// <summary>
    /// Moves the times of the buzzer on by the time spent offline: an arbitration window in progress goes on with the time
    /// it had left, and the buzzes it retained keep their order.
    /// </summary>
    /// <inheritdoc />
    public override RoundTransition ResumeRound(BuzzerRound round, GameState game, TimeSpan shift, GameContext context)
    {
        ArgumentNullException.ThrowIfNull(round);
        return Apply(round, round.Buzzer.Resume(shift));
    }

    /// <inheritdoc />
    public override PlayerRoundView ProjectForPlayer(BuzzerRound round, GameState game, Player player)
    {
        ArgumentNullException.ThrowIfNull(round);
        ArgumentNullException.ThrowIfNull(player);

        var buzzer = round.Buzzer;
        var state =
            round.Phase == BuzzerPhase.Revealed ? BuzzerButtonState.Closed
            : buzzer.Blocked.Contains(player.Id) ? BuzzerButtonState.Blocked
            : round.Phase is BuzzerPhase.Ready or BuzzerPhase.Closed ? BuzzerButtonState.Closed
            : buzzer.Winner == player.Id ? BuzzerButtonState.Won
            : buzzer.Winner is not null ? BuzzerButtonState.Lost

            // Whether this player buzzed, never whether the others did: the arbitration is not over.
            : buzzer.Presses.Any(press => press.PlayerId == player.Id) ? BuzzerButtonState.Buzzed
            : BuzzerButtonState.Open;
        return new BuzzerPlayerView(
            round.QuestionNumber,
            round.Descriptor.Questions.Length,
            buzzer.Opening,
            state,
            WinnerOf(round, game),
            round.Revealed ? PointsOf(round, player.Id) : null);
    }

    /// <inheritdoc />
    public override DisplayRoundView ProjectForDisplay(BuzzerRound round, GameState game)
    {
        ArgumentNullException.ThrowIfNull(round);
        ArgumentNullException.ThrowIfNull(game);

        // Nothing the TV screen does not show yet is sent: anyone may read its snapshots.
        var asked = round.Shown || round.Revealed;
        return new BuzzerDisplayView(
            round.QuestionNumber,
            round.Descriptor.Questions.Length,
            PhaseOf(round),
            asked ? round.Question.Text : null,
            asked && round.Question.Image is { } image ? game.Media.UrlOf(image) : null,
            WinnerOf(round, game),
            round.Revealed ? round.Question.Answer : null,
            FoundByOf(round, game));
    }

    /// <inheritdoc />
    public override GameMasterRoundView ProjectForGameMaster(BuzzerRound round, GameState game)
    {
        ArgumentNullException.ThrowIfNull(round);
        return new BuzzerGameMasterView(
            round.QuestionNumber,
            round.Descriptor.Questions.Length,
            PhaseOf(round),
            round.Buzzer.Opening,
            round.Question.Text,
            round.Shown || round.Revealed,
            round.Question.Answer,
            WinnerOf(round, game),
            FoundByOf(round, game));
    }

    /// <summary>
    /// The question in progress among those of the round.
    /// </summary>
    /// <inheritdoc />
    public override RoundStep? StepOf(BuzzerRound round)
    {
        ArgumentNullException.ThrowIfNull(round);
        return new RoundStep(round.QuestionNumber, round.Descriptor.Questions.Length);
    }

    /// <summary>
    /// One step per question.
    /// </summary>
    /// <inheritdoc />
    public override int CountPreviewSteps(BuzzerRoundDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        return descriptor.Questions.Length;
    }

    /// <summary>
    /// The question revealed with its answer, found by nobody.
    /// </summary>
    /// <inheritdoc />
    public override RoundPreview Preview(BuzzerRoundDescriptor descriptor, int stepIndex, DateTimeOffset? excerptStartsAt) =>
        new(new BuzzerRound(descriptor, stepIndex) { Shown = true, Revealed = true }, HasExcerpt: false);

    /// <summary>
    /// The number of the question the image illustrates: the question in progress when it does, since the TV screen shows
    /// only its image, or else the first one of the round.
    /// </summary>
    /// <inheritdoc />
    public override int? LocateMedia(BuzzerRound round, MediaPath media)
    {
        ArgumentNullException.ThrowIfNull(round);
        if (round.Question.Image == media)
        {
            return round.QuestionNumber;
        }

        var questions = round.Descriptor.Questions;
        for (var index = 0; index < questions.Length; index++)
        {
            if (questions[index].Image == media)
            {
                return index + 1;
            }
        }

        return null;
    }

    /// <summary>
    /// Asks the question announced: its buzzer opens to every player, and it shows on the TV screen unless the game master
    /// reads it aloud first.
    /// </summary>
    private static RoundTransition AskQuestion(BuzzerRound round, BuzzerAskQuestion ask, GameContext context)
    {
        RejectionReason? rejection =
            ask.QuestionNumber != round.QuestionNumber ? RejectionReason.QuestionMismatch
            : round.Phase != BuzzerPhase.Ready ? RejectionReason.PhaseMismatch
            : null;
        return rejection is { } reason
            ? RoundTransition.Rejected(round, reason)
            : new(round with { Buzzer = round.Buzzer.Open(context.Now), Shown = ask.ShowQuestion }, []);
    }

    /// <summary>
    /// Shows on the TV screen the question asked with its buzzer open but the question hidden, whoever has the hand.
    /// </summary>
    private static RoundTransition ShowQuestion(BuzzerRound round, BuzzerShowQuestion show)
    {
        RejectionReason? rejection =
            show.QuestionNumber != round.QuestionNumber ? RejectionReason.QuestionMismatch
            : round.Shown || round.Phase is BuzzerPhase.Ready or BuzzerPhase.Revealed ? RejectionReason.PhaseMismatch
            : null;
        return rejection is { } reason ? RoundTransition.Rejected(round, reason) : new(round with { Shown = true }, []);
    }

    /// <summary>
    /// Judges the answer of the player who has the hand. A correct one wins the points of the round, awarded now, and
    /// reveals the answer. A wrong one blocks the player for the question and opens the buzzer anew to the others, or
    /// leaves it closed once every connected player is blocked: nobody is left to buzz.
    /// </summary>
    private static RoundTransition Judge(BuzzerRound round, BuzzerJudge judge, GameState game, GameContext context)
    {
        RejectionReason? rejection =
            judge.QuestionNumber != round.QuestionNumber ? RejectionReason.QuestionMismatch
            : round.Phase != BuzzerPhase.Answering ? RejectionReason.PhaseMismatch

            // Judged already, the buzzer reopened and somebody else has the hand: this judgment is not about them.
            : judge.Opening != round.Buzzer.Opening ? RejectionReason.BuzzerOpeningMismatch
            : null;
        if (rejection is { } reason)
        {
            return RoundTransition.Rejected(round, reason);
        }

        var winner = round.Buzzer.Winner!.Value;
        if (judge.Correct)
        {
            var revealed = round with { Buzzer = round.Buzzer.Close(), Revealed = true, FoundBy = winner };
            return new(revealed, []) { Points = ImmutableDictionary<PlayerId, int>.Empty.Add(winner, round.Descriptor.Points) };
        }

        var blocked = round.Buzzer.Block(winner);
        var anybodyLeft = game.Players.Any(player => player.IsConnected && !blocked.Blocked.Contains(player.Id));
        return new(round with { Buzzer = anybodyLeft ? blocked.Reopen(context.Now) : blocked.Close() }, []);
    }

    /// <summary>
    /// Reveals the expected answer of the question asked, without points, whoever has the hand: the buzzer closes, and an
    /// arbitration window in progress is abandoned.
    /// </summary>
    private static RoundTransition RevealAnswer(BuzzerRound round, BuzzerRevealAnswer reveal)
    {
        RejectionReason? rejection =
            reveal.QuestionNumber != round.QuestionNumber ? RejectionReason.QuestionMismatch
            : round.Phase is BuzzerPhase.Ready or BuzzerPhase.Revealed ? RejectionReason.PhaseMismatch
            : null;
        if (rejection is { } reason)
        {
            return RoundTransition.Rejected(round, reason);
        }

        var revealed = round with { Buzzer = round.Buzzer.Close(), Revealed = true };
        return new(revealed, round.Phase == BuzzerPhase.Arbitrating ? [new CancelTimer(Buzzers.Buzzer.ArbitrationTimer)] : []);
    }

    /// <summary>
    /// Moves on from the revealed question: announces the next one, its buzzer closed, or ends the round after the last
    /// one, the round then staying on it.
    /// </summary>
    private static RoundTransition NextQuestion(BuzzerRound round, BuzzerNextQuestion next)
    {
        RejectionReason? rejection =
            next.QuestionNumber != round.QuestionNumber ? RejectionReason.QuestionMismatch
            : round.Phase != BuzzerPhase.Revealed ? RejectionReason.PhaseMismatch
            : null;
        if (rejection is { } reason)
        {
            return RoundTransition.Rejected(round, reason);
        }

        return round.QuestionIndex == round.Descriptor.Questions.Length - 1
            ? new(round, []) { IsFinished = true }
            : new(new BuzzerRound(round.Descriptor, round.QuestionIndex + 1), []);
    }

    /// <summary>
    /// Hands a buzz on the question in progress to its buzzer, which judges it.
    /// </summary>
    private static RoundTransition Buzz(BuzzerRound round, PlayerId player, BuzzerBuzz buzz, DateTimeOffset receivedAt, GameContext context) =>
        buzz.QuestionNumber != round.QuestionNumber
            ? RoundTransition.Rejected(round, RejectionReason.QuestionMismatch)
            : Apply(round, round.Buzzer.Buzz(player, buzz.Opening, buzz.PressedAt, receivedAt, context));

    /// <summary>
    /// The round with the buzzer a transition produced, or the very same round when the buzzer rejected the input.
    /// </summary>
    private static RoundTransition Apply(BuzzerRound round, BuzzerTransition transition) =>
        transition.Rejection is { } reason
            ? RoundTransition.Rejected(round, reason)
            : new(round with { Buzzer = transition.Buzzer }, transition.Effects);

    /// <summary>
    /// The nickname of the player who has the hand, public once designated.
    /// </summary>
    private static string? WinnerOf(BuzzerRound round, GameState game) =>
        round.Buzzer.Winner is { } winner ? NicknameOf(game, winner) : null;

    /// <summary>
    /// The nickname of the player who found the answer, public once revealed.
    /// </summary>
    private static string? FoundByOf(BuzzerRound round, GameState game) =>
        round.FoundBy is { } player ? NicknameOf(game, player) : null;

    /// <summary>
    /// Players are never removed: whoever buzzed is still registered, under their current nickname.
    /// </summary>
    private static string NicknameOf(GameState game, PlayerId id) => game.Players.First(player => player.Id == id).Nickname;

    /// <summary>
    /// What a player earned with the revealed question: the points of the round if they found it, 0 otherwise.
    /// </summary>
    private static int PointsOf(BuzzerRound round, PlayerId player) => round.FoundBy == player ? round.Descriptor.Points : 0;

    private static BuzzerQuestionPhase PhaseOf(BuzzerRound round) => round.Phase switch
    {
        BuzzerPhase.Ready => BuzzerQuestionPhase.Ready,

        // The arbitration window stays invisible: nobody learns that somebody buzzed before the winner is designated.
        BuzzerPhase.Open or BuzzerPhase.Arbitrating => BuzzerQuestionPhase.Open,
        BuzzerPhase.Answering => BuzzerQuestionPhase.Answering,
        BuzzerPhase.Closed => BuzzerQuestionPhase.Closed,
        BuzzerPhase.Revealed => BuzzerQuestionPhase.Revealed,
        _ => throw new InvalidOperationException($"Phase {round.Phase} has no projection."),
    };
}
