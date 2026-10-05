using System.Collections.Immutable;
using PartyGame.Contracts;
using PartyGame.Contracts.Buzzer;
using PartyGame.Contracts.Packs;
using PartyGame.Engine.Buzzers;
using PartyGame.Engine.Inputs;

namespace PartyGame.Engine.Modes.Buzzer;

/// <summary>
/// Plays the rounds of buzzer questions of the packs: the game master asks each question, which opens the buzzer, and
/// the first player who pressed has the hand.
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
            buzzer.Blocked.Contains(player.Id) ? BuzzerButtonState.Blocked
            : round.Phase == BuzzerPhase.Ready ? BuzzerButtonState.Closed
            : buzzer.Winner == player.Id ? BuzzerButtonState.Won
            : buzzer.Winner is not null ? BuzzerButtonState.Lost

            // Whether this player buzzed, never whether the others did: the arbitration is not over.
            : buzzer.Presses.Any(press => press.PlayerId == player.Id) ? BuzzerButtonState.Buzzed
            : BuzzerButtonState.Open;
        return new BuzzerPlayerView(round.QuestionNumber, round.Descriptor.Questions.Length, buzzer.Opening, state, WinnerOf(round, game));
    }

    /// <inheritdoc />
    public override DisplayRoundView ProjectForDisplay(BuzzerRound round, GameState game)
    {
        ArgumentNullException.ThrowIfNull(round);
        ArgumentNullException.ThrowIfNull(game);

        // Nothing the TV screen does not show yet is sent: anyone may read its snapshots.
        var asked = round.Phase != BuzzerPhase.Ready;
        return new BuzzerDisplayView(
            round.QuestionNumber,
            round.Descriptor.Questions.Length,
            PhaseOf(round),
            asked ? round.Question.Text : null,
            asked && round.Question.Image is { } image ? game.Media.UrlOf(image) : null,
            WinnerOf(round, game));
    }

    /// <inheritdoc />
    public override GameMasterRoundView ProjectForGameMaster(BuzzerRound round, GameState game)
    {
        ArgumentNullException.ThrowIfNull(round);
        return new BuzzerGameMasterView(
            round.QuestionNumber,
            round.Descriptor.Questions.Length,
            PhaseOf(round),
            round.Question.Text,
            round.Question.Answer,
            WinnerOf(round, game));
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
    /// Asks the question announced: it shows on the TV screen, and its buzzer opens to every player.
    /// </summary>
    private static RoundTransition AskQuestion(BuzzerRound round, BuzzerAskQuestion ask, GameContext context)
    {
        RejectionReason? rejection =
            ask.QuestionNumber != round.QuestionNumber ? RejectionReason.QuestionMismatch
            : round.Phase != BuzzerPhase.Ready ? RejectionReason.PhaseMismatch
            : null;
        return rejection is { } reason
            ? RoundTransition.Rejected(round, reason)
            : new(round with { Buzzer = round.Buzzer.Open(context.Now) }, []);
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
    /// The nickname of the player who has the hand, public once designated. Players are never removed: the winner is still
    /// registered, under their current nickname.
    /// </summary>
    private static string? WinnerOf(BuzzerRound round, GameState game) =>
        round.Buzzer.Winner is { } winner ? game.Players.First(player => player.Id == winner).Nickname : null;

    private static BuzzerQuestionPhase PhaseOf(BuzzerRound round) => round.Phase switch
    {
        BuzzerPhase.Ready => BuzzerQuestionPhase.Ready,

        // The arbitration window stays invisible: nobody learns that somebody buzzed before the winner is designated.
        BuzzerPhase.Open or BuzzerPhase.Arbitrating => BuzzerQuestionPhase.Open,
        BuzzerPhase.Answering => BuzzerQuestionPhase.Answering,
        _ => throw new InvalidOperationException($"Phase {round.Phase} has no projection."),
    };
}
