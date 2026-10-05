using System.Collections.Immutable;
using System.Globalization;
using PartyGame.Contracts;
using PartyGame.Contracts.OpenQuestion;
using PartyGame.Contracts.Packs;
using PartyGame.Engine.Effects;
using PartyGame.Engine.Inputs;

namespace PartyGame.Engine.Modes.OpenQuestion;

/// <summary>
/// Plays the rounds of open questions of the packs.
/// </summary>
/// <remarks>
/// For now, the answers lock and the game master moves on by skipping the question: their validation and their reveal come
/// with US-E16-03 and US-E16-04.
/// </remarks>
public sealed class OpenQuestionMode : GameMode<OpenQuestionRoundDescriptor, OpenQuestionRound>
{
    /// <summary>
    /// The timer that locks the answers at the end of the countdown, unless every participant answered before: it starts
    /// once the question shows.
    /// </summary>
    public static readonly TimerId AnswersTimer = new("openquestion-answers");

    /// <summary>
    /// Checks that the expected answer and the variants of each question could be typed and matched: no longer than the
    /// <c>maxLength</c> of the round, not empty once normalized, digits only for a numeric question, and no variant
    /// normalized to the same as another.
    /// </summary>
    /// <inheritdoc />
    public override ImmutableArray<PackProblem> Validate(OpenQuestionRoundDescriptor descriptor, string path)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        var problems = ImmutableArray.CreateBuilder<PackProblem>();
        for (var questionIndex = 0; questionIndex < descriptor.Questions.Length; questionIndex++)
        {
            var question = descriptor.Questions[questionIndex];
            var questionPath = string.Create(CultureInfo.InvariantCulture, $"{path}.questions[{questionIndex}]");
            var normalized = new HashSet<string>(StringComparer.Ordinal);

            Check(question.Answer, $"{questionPath}.answer", isVariant: false);
            for (var variantIndex = 0; variantIndex < question.AcceptedAnswers.Length; variantIndex++)
            {
                Check(question.AcceptedAnswers[variantIndex], string.Create(CultureInfo.InvariantCulture, $"{questionPath}.acceptedAnswers[{variantIndex}]"), isVariant: true);
            }

            void Check(string answer, string answerPath, bool isVariant)
            {
                if (answer.Length == 0 || answer.Length > descriptor.MaxLength)
                {
                    // The loading already reports an expected answer out of the bounds of its property.
                    if (isVariant || answer.Length is > 0 and <= OpenQuestionRoundDescriptor.MaxMaxLength)
                    {
                        problems.Add(Problem(PackProblemCode.OpenQuestionAnswerLengthOutOfRange, answerPath, "max", descriptor.MaxLength.ToString(CultureInfo.InvariantCulture)));
                    }

                    return;
                }

                var key = OpenAnswers.Normalize(answer);
                if (question.InputMode == OpenQuestionInputMode.Numeric && !answer.All(char.IsAsciiDigit))
                {
                    problems.Add(Problem(PackProblemCode.OpenQuestionAnswerNotNumeric, answerPath, "answer", answer));
                }
                else if (key.Length == 0)
                {
                    problems.Add(Problem(PackProblemCode.OpenQuestionAnswerEmpty, answerPath, "answer", answer));
                }
                else if (!normalized.Add(key))
                {
                    problems.Add(Problem(PackProblemCode.OpenQuestionAnswerDuplicated, answerPath, "answer", answer));
                }
            }
        }

        return problems.ToImmutable();
    }

    /// <summary>
    /// Presents the first question of the round.
    /// </summary>
    /// <inheritdoc />
    public override RoundTransition Start(OpenQuestionRoundDescriptor descriptor, GameState game, GameContext context) =>
        new(new OpenQuestionRound(descriptor, 0, OpenQuestionPhase.Presentation), []);

    /// <summary>
    /// Plays the intents of the round, each aimed at the question it names. The answers open when the question shows, and
    /// lock once every participant answered, or when their timer elapses.
    /// </summary>
    /// <inheritdoc />
    public override RoundTransition Handle(OpenQuestionRound round, GameInput input, GameState game, GameContext context)
    {
        ArgumentNullException.ThrowIfNull(round);
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(context);

        return input switch
        {
            GameMasterRoundInput { RoundIntent: OpenQuestionShowQuestion show } => ShowQuestion(round, show, game, context),
            GameMasterRoundInput { RoundIntent: OpenQuestionSkipQuestion skip } => SkipQuestion(round, skip),
            PlayerRoundInput { RoundIntent: OpenQuestionSubmitAnswer answer } submitted =>
                SubmitAnswer(round, submitted.PlayerId, answer, submitted.ReceivedAt),
            TimerElapsed timer => CloseAnswers(round, timer),
            _ => RoundTransition.Rejected(round, RejectionReason.IntentUnsupported),
        };
    }

    /// <summary>
    /// Moves the deadline of the answers and the time of reception of each answer on by the time spent offline: the
    /// countdown goes on with the time it had left. A countdown in progress is scheduled again.
    /// </summary>
    /// <inheritdoc />
    public override RoundTransition ResumeRound(OpenQuestionRound round, GameState game, TimeSpan shift, GameContext context)
    {
        ArgumentNullException.ThrowIfNull(round);

        var resumed = round with
        {
            AnswersCloseAt = round.AnswersCloseAt + shift,
            Answers = round.Answers.ToImmutableDictionary(entry => entry.Key, entry => entry.Value with { ReceivedAt = entry.Value.ReceivedAt + shift }),
        };
        return round.Phase == OpenQuestionPhase.Answering
            ? new(resumed, [new ScheduleTimer(AnswersTimer, resumed.AnswersCloseAt!.Value)])
            : new(resumed, []);
    }

    /// <inheritdoc />
    public override PlayerRoundView ProjectForPlayer(OpenQuestionRound round, GameState game, Player player)
    {
        ArgumentNullException.ThrowIfNull(round);
        ArgumentNullException.ThrowIfNull(player);
        return new OpenQuestionPlayerView(
            round.QuestionNumber,
            round.Descriptor.Questions.Length,
            PhaseOf(round),
            round.Question.InputMode == OpenQuestionInputMode.Numeric,
            round.Descriptor.MaxLength,
            CloseTimeOf(round),

            // Whoever is registered before the question shows takes part once it opens the answers.
            round.Phase == OpenQuestionPhase.Presentation || round.Participants.Contains(player.Id),
            round.Answers.TryGetValue(player.Id, out var answer) ? answer.Text : null);
    }

    /// <inheritdoc />
    public override DisplayRoundView ProjectForDisplay(OpenQuestionRound round, GameState game)
    {
        ArgumentNullException.ThrowIfNull(round);
        ArgumentNullException.ThrowIfNull(game);
        var shown = round.Phase != OpenQuestionPhase.Presentation;
        return new OpenQuestionDisplayView(
            round.QuestionNumber,
            round.Descriptor.Questions.Length,
            PhaseOf(round),

            // Nothing the TV screen does not show yet is sent: anyone may read its snapshots.
            shown ? round.Question.Text : null,
            shown && round.Question.Image is { } image ? game.Media.UrlOf(image) : null,
            CloseTimeOf(round),

            // How many answered, never what.
            round.Answers.Count,
            round.Participants.Length);
    }

    /// <inheritdoc />
    public override GameMasterRoundView ProjectForGameMaster(OpenQuestionRound round, GameState game)
    {
        ArgumentNullException.ThrowIfNull(round);
        ArgumentNullException.ThrowIfNull(game);
        return new OpenQuestionGameMasterView(
            round.QuestionNumber,
            round.Descriptor.Questions.Length,
            PhaseOf(round),
            round.Question.Text,
            round.Phase != OpenQuestionPhase.Presentation,
            round.Question.Answer,
            round.Question.AcceptedAnswers,
            CloseTimeOf(round),
            [
                .. game.Players
                    .Where(player => round.Participants.Contains(player.Id))
                    .Select(player => new OpenQuestionGameMasterAnswer(
                        player.Id,
                        player.Nickname,
                        round.Answers.TryGetValue(player.Id, out var answer) ? answer.Text : null)),
            ]);
    }

    /// <summary>
    /// The question in progress among those of the round.
    /// </summary>
    /// <inheritdoc />
    public override RoundStep? StepOf(OpenQuestionRound round)
    {
        ArgumentNullException.ThrowIfNull(round);
        return new RoundStep(round.QuestionNumber, round.Descriptor.Questions.Length);
    }

    /// <summary>
    /// The number of the question the image illustrates: the question in progress when it does, since the TV screen shows
    /// only its image, or else the first one of the round.
    /// </summary>
    /// <inheritdoc />
    public override int? LocateMedia(OpenQuestionRound round, MediaPath media)
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
    /// Shows the question presented on the TV screen, with its image: the answers open to the players registered now, and
    /// their countdown starts.
    /// </summary>
    private static RoundTransition ShowQuestion(OpenQuestionRound round, OpenQuestionShowQuestion show, GameState game, GameContext context)
    {
        RejectionReason? rejection =
            show.QuestionNumber != round.QuestionNumber ? RejectionReason.QuestionMismatch
            : round.Phase != OpenQuestionPhase.Presentation ? RejectionReason.PhaseMismatch
            : null;
        if (rejection is { } reason)
        {
            return RoundTransition.Rejected(round, reason);
        }

        // Fixed now, so that the last answer expected is known: a player who joins later plays the next question.
        var closeAt = context.Now.AddSeconds(round.Question.AnswerSeconds ?? round.Descriptor.AnswerSeconds);
        var shown = round with { Participants = [.. game.Players.Select(player => player.Id)], AnswersCloseAt = closeAt };
        return shown.Participants.IsEmpty
            ? new(shown with { Phase = OpenQuestionPhase.Locked }, [])
            : new(shown with { Phase = OpenQuestionPhase.Answering }, [new ScheduleTimer(AnswersTimer, closeAt)]);
    }

    /// <summary>
    /// Gives up the question in progress: its answers are ignored, and its countdown stops if it runs.
    /// </summary>
    private static RoundTransition SkipQuestion(OpenQuestionRound round, OpenQuestionSkipQuestion skip)
    {
        if (skip.QuestionNumber != round.QuestionNumber)
        {
            return RoundTransition.Rejected(round, RejectionReason.QuestionMismatch);
        }

        ImmutableArray<Effect> effects = round.Phase == OpenQuestionPhase.Answering ? [new CancelTimer(AnswersTimer)] : [];
        if (round.QuestionIndex == round.Descriptor.Questions.Length - 1)
        {
            return new(round with { SkippedQuestions = round.SkippedQuestions.Add(round.QuestionIndex) }, effects) { IsFinished = true };
        }

        var next = new OpenQuestionRound(round.Descriptor, round.QuestionIndex + 1, OpenQuestionPhase.Presentation)
        {
            SkippedQuestions = round.SkippedQuestions.Add(round.QuestionIndex),
        };
        return new(next, effects);
    }

    /// <summary>
    /// Locks the answers at the end of the countdown. The timer of answers already locked, or of another question, is
    /// obsolete.
    /// </summary>
    private static RoundTransition CloseAnswers(OpenQuestionRound round, TimerElapsed timer) =>
        timer.TimerId != AnswersTimer || round.Phase != OpenQuestionPhase.Answering || timer.DueAt != round.AnswersCloseAt
            ? RoundTransition.Rejected(round, RejectionReason.UnexpectedTimer)
            : new(round with { Phase = OpenQuestionPhase.Locked }, []);

    /// <summary>
    /// Records the first answer of a participant, received before the answers close, as typed: never truncated, rejected
    /// when too long or empty once normalized. Locks the answers once every participant answered: nobody is left to wait
    /// for.
    /// </summary>
    private static RoundTransition SubmitAnswer(OpenQuestionRound round, PlayerId playerId, OpenQuestionSubmitAnswer answer, DateTimeOffset receivedAt)
    {
        RejectionReason? rejection =
            answer.QuestionNumber != round.QuestionNumber ? RejectionReason.QuestionMismatch
            : round.Phase != OpenQuestionPhase.Answering ? RejectionReason.PhaseMismatch

            // Received once closed, although the loop has not handled the timer yet: the time of reception decides.
            : receivedAt >= round.AnswersCloseAt ? RejectionReason.AnswerTooLate
            : !round.Participants.Contains(playerId) ? RejectionReason.NotParticipating
            : round.Answers.ContainsKey(playerId) ? RejectionReason.AlreadyAnswered
            : answer.Answer.Length > round.Descriptor.MaxLength ? RejectionReason.AnswerTooLong
            : OpenAnswers.Normalize(answer.Answer).Length == 0 ? RejectionReason.AnswerEmpty
            : null;
        if (rejection is { } reason)
        {
            return RoundTransition.Rejected(round, reason);
        }

        var answered = round with { Answers = round.Answers.Add(playerId, new OpenAnswer(answer.Answer, receivedAt)) };
        return answered.Answers.Count == answered.Participants.Length
            ? new(answered with { Phase = OpenQuestionPhase.Locked }, [new CancelTimer(AnswersTimer)])
            : new(answered, []);
    }

    /// <summary>
    /// When the answers close, while their countdown runs: the screens count down to it.
    /// </summary>
    private static long? CloseTimeOf(OpenQuestionRound round) =>
        round.Phase == OpenQuestionPhase.Answering ? round.AnswersCloseAt?.ToUnixTimeMilliseconds() : null;

    private static OpenQuestionQuestionPhase PhaseOf(OpenQuestionRound round) => round.Phase switch
    {
        OpenQuestionPhase.Presentation => OpenQuestionQuestionPhase.Presentation,
        OpenQuestionPhase.Answering => OpenQuestionQuestionPhase.Answering,
        OpenQuestionPhase.Locked => OpenQuestionQuestionPhase.Locked,
        _ => throw new InvalidOperationException($"Phase {round.Phase} has no projection."),
    };

    private static PackProblem Problem(PackProblemCode code, string path, string name, string value) =>
        new(code, PackDescriptor.FileName, path, ImmutableDictionary<string, string>.Empty.Add(name, value));
}
