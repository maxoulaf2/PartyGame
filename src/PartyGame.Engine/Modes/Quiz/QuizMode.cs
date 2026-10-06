using System.Collections.Immutable;
using System.Globalization;
using PartyGame.Contracts;
using PartyGame.Contracts.Packs;
using PartyGame.Contracts.Quiz;
using PartyGame.Engine.Effects;
using PartyGame.Engine.Inputs;
using PartyGame.Engine.Text;

namespace PartyGame.Engine.Modes.Quiz;

/// <summary>
/// Plays the multiple-choice quiz rounds of the packs.
/// </summary>
public sealed class QuizMode : GameMode<QuizRoundDescriptor, QuizRound>
{
    /// <summary>
    /// The timer that locks the answers at the end of the countdown, unless every participant answered before: it starts
    /// once the last choice shows.
    /// </summary>
    public static readonly TimerId AnswersTimer = new("quiz-answers");

    /// <summary>
    /// Checks that each question has exactly one correct choice, and no two choices players would take for the same.
    /// </summary>
    /// <inheritdoc />
    public override ImmutableArray<PackProblem> Validate(QuizRoundDescriptor descriptor, string path)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        var problems = ImmutableArray.CreateBuilder<PackProblem>();
        for (var questionIndex = 0; questionIndex < descriptor.Questions.Length; questionIndex++)
        {
            var question = descriptor.Questions[questionIndex];
            var questionPath = string.Create(CultureInfo.InvariantCulture, $"{path}.questions[{questionIndex}]");

            switch (question.Choices.Count(choice => choice.Correct))
            {
                case 0:
                    problems.Add(Problem(PackProblemCode.QuizCorrectChoiceMissing, questionPath));
                    break;
                case > 1:
                    problems.Add(Problem(PackProblemCode.QuizCorrectChoiceDuplicated, questionPath));
                    break;
            }

            var keys = new HashSet<string>(StringComparer.Ordinal);
            for (var choiceIndex = 0; choiceIndex < question.Choices.Length; choiceIndex++)
            {
                var choice = question.Choices[choiceIndex];
                if (!keys.Add(TextComparison.Key(choice.Text)))
                {
                    problems.Add(Problem(
                        PackProblemCode.QuizChoiceDuplicated,
                        string.Create(CultureInfo.InvariantCulture, $"{questionPath}.choices[{choiceIndex}]"),
                        ImmutableDictionary<string, string>.Empty.Add("choice", choice.Text)));
                }
            }
        }

        return problems.ToImmutable();
    }

    /// <summary>
    /// Presents the first question of the round.
    /// </summary>
    /// <inheritdoc />
    public override RoundTransition Start(QuizRoundDescriptor descriptor, GameState game, GameContext context)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(context);
        return new(Present(descriptor, 0, context.Random), []);
    }

    /// <summary>
    /// Plays the intents of the quiz, each aimed at the question it names, and the part of it it shows. The answers open
    /// with the first choice shown, and lock once every participant answered and every choice is shown, or when their
    /// timer elapses.
    /// </summary>
    /// <inheritdoc />
    public override RoundTransition Handle(QuizRound round, GameInput input, GameState game, GameContext context)
    {
        ArgumentNullException.ThrowIfNull(round);
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(context);

        return input switch
        {
            GameMasterRoundInput { RoundIntent: QuizShowQuestion show } => ShowQuestion(round, show),
            GameMasterRoundInput { RoundIntent: QuizShowChoice show } => ShowChoice(round, show, game, context),
            GameMasterRoundInput { RoundIntent: QuizRevealAnswer reveal } => RevealAnswer(round, reveal, game),
            GameMasterRoundInput { RoundIntent: QuizNextQuestion next } => NextQuestion(round, next, context),
            GameMasterRoundInput { RoundIntent: QuizSkipQuestion skip } => SkipQuestion(round, skip, context),
            PlayerRoundInput { RoundIntent: QuizSubmitAnswer answer } submitted =>
                SubmitAnswer(round, submitted.PlayerId, answer, submitted.ReceivedAt),
            TimerElapsed timer => CloseAnswers(round, timer),
            _ => RoundTransition.Rejected(round, RejectionReason.IntentUnsupported),
        };
    }

    /// <summary>
    /// Moves the deadline of the answers and the time of reception of each answer on by the time spent offline: the
    /// countdown goes on with the time it had left, and the answers already received keep their speed bonus. A countdown
    /// in progress is scheduled again.
    /// </summary>
    /// <inheritdoc />
    public override RoundTransition ResumeRound(QuizRound round, GameState game, TimeSpan shift, GameContext context)
    {
        ArgumentNullException.ThrowIfNull(round);

        var resumed = round with
        {
            AnswersCloseAt = round.AnswersCloseAt + shift,
            Answers = round.Answers.ToImmutableDictionary(entry => entry.Key, entry => entry.Value with { ReceivedAt = entry.Value.ReceivedAt + shift }),
        };
        return round.Phase == QuizPhase.Answering
            ? new(resumed, [new ScheduleTimer(AnswersTimer, resumed.AnswersCloseAt!.Value)])
            : new(resumed, []);
    }

    /// <inheritdoc />
    public override PlayerRoundView ProjectForPlayer(QuizRound round, GameState game, Player player)
    {
        ArgumentNullException.ThrowIfNull(round);
        ArgumentNullException.ThrowIfNull(player);
        return new QuizPlayerView(
            round.QuestionNumber,
            round.Descriptor.Questions.Length,
            PhaseOf(round),
            [.. ShownChoicesOf(round).Select(shown => shown.Letter)],
            round.ShownChoiceCount,
            CloseTimeOf(round),

            // Whoever is registered before the first choice shows takes part once it opens the answers.
            !AnswersOpened(round) || round.Participants.Contains(player.Id),
            round.Answers.TryGetValue(player.Id, out var answer) ? answer.Choice : null,
            round.Phase == QuizPhase.Revealed ? CorrectLetterOf(round) : null,
            round.Phase == QuizPhase.Revealed ? VerdictOf(round, player.Id) : null,
            PointsOf(round, player.Id));
    }

    /// <inheritdoc />
    public override DisplayRoundView ProjectForDisplay(QuizRound round, GameState game)
    {
        ArgumentNullException.ThrowIfNull(round);
        ArgumentNullException.ThrowIfNull(game);
        return new QuizDisplayView(
            round.QuestionNumber,
            round.Descriptor.Questions.Length,
            PhaseOf(round),

            // Nothing the TV screen does not show yet is sent: anyone may read its snapshots.
            round.QuestionShown ? round.Question.Text : null,
            round.QuestionShown && round.Question.Image is { } image ? game.Media.UrlOf(image) : null,
            PublicChoicesOf(round),
            round.ChoiceOrder.Length,
            CloseTimeOf(round),

            // How many answered, never what: the choices stay secret until the reveal.
            round.Answers.Count,
            round.Participants.Length,
            round.Phase == QuizPhase.Revealed
                ? new QuizDisplayReveal(
                    CorrectLetterOf(round),
                    [.. ParticipantsOf(round, game).Select(p => new QuizRevealedAnswer(p.Player.Id, p.Player.Nickname, p.Choice))])
                : null);
    }

    /// <inheritdoc />
    public override GameMasterRoundView ProjectForGameMaster(QuizRound round, GameState game)
    {
        ArgumentNullException.ThrowIfNull(round);
        ArgumentNullException.ThrowIfNull(game);
        return new QuizGameMasterView(
            round.QuestionNumber,
            round.Descriptor.Questions.Length,
            PhaseOf(round),
            round.Question.Text,
            round.QuestionShown,
            [
                .. ShownChoicesOf(round).Select(shown => new QuizGameMasterChoice(
                    shown.Letter,
                    shown.Choice.Text,
                    shown.Choice.Correct,
                    (int)shown.Letter < round.ShownChoiceCount,
                    round.Answers.Values.Count(answer => answer.Choice == shown.Letter))),
            ],
            CloseTimeOf(round),

            [
                .. ParticipantsOf(round, game).Select(p =>
                    new QuizGameMasterAnswer(p.Player.Id, p.Player.Nickname, p.Choice, PointsOf(round, p.Player.Id))),
            ]);
    }

    /// <summary>
    /// The question in progress among those of the round.
    /// </summary>
    /// <inheritdoc />
    public override RoundStep? StepOf(QuizRound round)
    {
        ArgumentNullException.ThrowIfNull(round);
        return new RoundStep(round.QuestionNumber, round.Descriptor.Questions.Length);
    }

    /// <summary>
    /// The number of the question the image illustrates: the question in progress when it does, since the TV screen shows
    /// only its image, or else the first one of the round.
    /// </summary>
    /// <inheritdoc />
    public override int? LocateMedia(QuizRound round, MediaPath media)
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
    /// One step per question.
    /// </summary>
    /// <inheritdoc />
    public override int CountPreviewSteps(QuizRoundDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        return descriptor.Questions.Length;
    }

    /// <summary>
    /// The question revealed, its choices all shown in the order of the descriptor.
    /// </summary>
    /// <inheritdoc />
    public override RoundPreview Preview(QuizRoundDescriptor descriptor, int stepIndex, DateTimeOffset? excerptStartsAt)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        var choices = descriptor.Questions[stepIndex].Choices.Length;
        var round = new QuizRound(descriptor, stepIndex, QuizPhase.Revealed, [.. Enumerable.Range(0, choices)])
        {
            QuestionShown = true,
            ShownChoiceCount = choices,
        };
        return new(round, HasExcerpt: false);
    }

    /// <summary>
    /// Presents a question of the round, its choices in the order of the descriptor, or shuffled when the round asks for
    /// it. The shuffle draws from the generator of the context, so that it is reproducible.
    /// </summary>
    private static QuizRound Present(QuizRoundDescriptor descriptor, int questionIndex, Random random)
    {
        var order = Enumerable.Range(0, descriptor.Questions[questionIndex].Choices.Length).ToArray();
        if (descriptor.ShuffleChoices)
        {
            random.Shuffle(order);
        }

        return new QuizRound(descriptor, questionIndex, QuizPhase.Presentation, [.. order]);
    }

    /// <summary>
    /// Shows the question presented on the TV screen, with its image, its choices still hidden.
    /// </summary>
    private static RoundTransition ShowQuestion(QuizRound round, QuizShowQuestion show)
    {
        RejectionReason? rejection =
            show.QuestionNumber != round.QuestionNumber ? RejectionReason.QuestionMismatch
            : round.Phase != QuizPhase.Presentation ? RejectionReason.PhaseMismatch
            : round.QuestionShown ? RejectionReason.PresentationStepMismatch
            : null;
        return rejection is { } reason
            ? RoundTransition.Rejected(round, reason)
            : new(round with { QuestionShown = true }, []);
    }

    /// <summary>
    /// Shows the next choice of the question presented on the TV screen, once the question and the choices before it are
    /// shown: the intent names it, so that a request sent twice shows it once. The players may choose it at once. The
    /// first choice opens the answers to the players registered now; the last one starts their countdown, or locks them
    /// if every participant answered already.
    /// </summary>
    private static RoundTransition ShowChoice(QuizRound round, QuizShowChoice show, GameState game, GameContext context)
    {
        RejectionReason? rejection =
            show.QuestionNumber != round.QuestionNumber ? RejectionReason.QuestionMismatch
            : round.Phase != QuizPhase.Presentation ? RejectionReason.PhaseMismatch
            : (int)show.Choice < 0 || (int)show.Choice >= round.ChoiceOrder.Length ? RejectionReason.ChoiceUnknown
            : !round.QuestionShown || (int)show.Choice != round.ShownChoiceCount ? RejectionReason.PresentationStepMismatch
            : null;
        if (rejection is { } reason)
        {
            return RoundTransition.Rejected(round, reason);
        }

        var shown = round with { ShownChoiceCount = round.ShownChoiceCount + 1 };
        if (!AnswersOpened(round))
        {
            // Fixed now, so that the last answer expected is known: a player who joins later plays the next question.
            shown = shown with { Participants = [.. game.Players.Select(player => player.Id)] };
        }

        if (shown.ShownChoiceCount < shown.ChoiceOrder.Length)
        {
            return new(shown, []);
        }

        if (EverybodyAnswered(shown))
        {
            return new(shown with { Phase = QuizPhase.Locked }, []);
        }

        var closeAt = context.Now.AddSeconds(round.Question.AnswerSeconds ?? round.Descriptor.AnswerSeconds);
        return new(shown with { Phase = QuizPhase.Answering, AnswersCloseAt = closeAt }, [new ScheduleTimer(AnswersTimer, closeAt)]);
    }

    /// <summary>
    /// Reveals the correct answer once the answers are locked. The points of the question are awarded now, never before,
    /// so that no score tells a correct answer ahead of time.
    /// </summary>
    private static RoundTransition RevealAnswer(QuizRound round, QuizRevealAnswer reveal, GameState game)
    {
        if (reveal.QuestionNumber != round.QuestionNumber)
        {
            return RoundTransition.Rejected(round, RejectionReason.QuestionMismatch);
        }

        if (round.Phase != QuizPhase.Locked)
        {
            return RoundTransition.Rejected(round, RejectionReason.PhaseMismatch);
        }

        var correct = CorrectLetterOf(round);
        var points = ParticipantsOf(round, game).ToImmutableDictionary(
            p => p.Player.Id,
            p => p.Choice == correct ? PointsFor(round, round.Answers[p.Player.Id]) : 0);
        return new(round with { Phase = QuizPhase.Revealed, Points = points }, []) { Points = points };
    }

    /// <summary>
    /// The points of a correct answer: those of the round, plus its speed bonus in proportion to the time left when the
    /// answer was received, rounded to the nearest integer: the whole bonus before the countdown starts, while the choices
    /// show. Computed in integers, so that the result is exact and reproducible.
    /// </summary>
    private static int PointsFor(QuizRound round, QuizAnswer answer)
    {
        var duration = TimeSpan.FromSeconds(round.Question.AnswerSeconds ?? round.Descriptor.AnswerSeconds).Ticks;

        // Kept within the countdown, which never started when everybody answered before the last choice showed.
        var left = round.AnswersCloseAt is { } closeAt ? Math.Clamp((closeAt - answer.ReceivedAt).Ticks, 0, duration) : duration;
        var bonus = ((2 * round.Descriptor.SpeedBonus * left) + duration) / (2 * duration);
        return round.Descriptor.Points + (int)bonus;
    }

    /// <summary>
    /// Moves on from the revealed question: to the next one, or to the end of the round after the last one.
    /// </summary>
    private static RoundTransition NextQuestion(QuizRound round, QuizNextQuestion next, GameContext context)
    {
        if (next.QuestionNumber != round.QuestionNumber)
        {
            return RoundTransition.Rejected(round, RejectionReason.QuestionMismatch);
        }

        if (round.Phase != QuizPhase.Revealed)
        {
            return RoundTransition.Rejected(round, RejectionReason.PhaseMismatch);
        }

        return MoveOn(round, context, []);
    }

    /// <summary>
    /// Gives up the question in progress before its reveal: its answers are ignored, and its countdown stops if it runs.
    /// Once revealed, the game master moves on to the next question instead.
    /// </summary>
    private static RoundTransition SkipQuestion(QuizRound round, QuizSkipQuestion skip, GameContext context)
    {
        if (skip.QuestionNumber != round.QuestionNumber)
        {
            return RoundTransition.Rejected(round, RejectionReason.QuestionMismatch);
        }

        if (round.Phase == QuizPhase.Revealed)
        {
            return RoundTransition.Rejected(round, RejectionReason.PhaseMismatch);
        }

        var skipped = round with { SkippedQuestions = round.SkippedQuestions.Add(round.QuestionIndex) };
        return MoveOn(skipped, context, round.Phase == QuizPhase.Answering ? [new CancelTimer(AnswersTimer)] : []);
    }

    /// <summary>
    /// Presents the question that follows the one in progress, or ends the round after its last one, the round then
    /// staying on it.
    /// </summary>
    private static RoundTransition MoveOn(QuizRound round, GameContext context, ImmutableArray<Effect> effects)
    {
        if (round.QuestionIndex == round.Descriptor.Questions.Length - 1)
        {
            return new(round, effects) { IsFinished = true };
        }

        var next = Present(round.Descriptor, round.QuestionIndex + 1, context.Random) with { SkippedQuestions = round.SkippedQuestions };
        return new(next, effects);
    }

    /// <summary>
    /// Locks the answers at the end of the countdown. The timer of answers already locked, or of another question, is
    /// obsolete.
    /// </summary>
    private static RoundTransition CloseAnswers(QuizRound round, TimerElapsed timer)
    {
        if (timer.TimerId != AnswersTimer || round.Phase != QuizPhase.Answering || timer.DueAt != round.AnswersCloseAt)
        {
            return RoundTransition.Rejected(round, RejectionReason.UnexpectedTimer);
        }

        return new(round with { Phase = QuizPhase.Locked }, []);
    }

    /// <summary>
    /// Records the first answer of a participant to a choice shown, received before the answers close, and locks the
    /// answers once every participant answered during the countdown: nobody is left to wait for. Before it, the last choice
    /// shown locks them.
    /// </summary>
    private static RoundTransition SubmitAnswer(QuizRound round, PlayerId playerId, QuizSubmitAnswer answer, DateTimeOffset receivedAt)
    {
        RejectionReason? rejection =
            answer.QuestionNumber != round.QuestionNumber ? RejectionReason.QuestionMismatch
            : !AnswersOpen(round) ? RejectionReason.PhaseMismatch

            // Received once closed, although the loop has not handled the timer yet: the time of reception decides.
            : receivedAt >= round.AnswersCloseAt ? RejectionReason.AnswerTooLate
            : !round.Participants.Contains(playerId) ? RejectionReason.NotParticipating
            : (int)answer.Choice < 0 || (int)answer.Choice >= round.ChoiceOrder.Length ? RejectionReason.ChoiceUnknown
            : (int)answer.Choice >= round.ShownChoiceCount ? RejectionReason.ChoiceHidden
            : round.Answers.ContainsKey(playerId) ? RejectionReason.AlreadyAnswered
            : null;
        if (rejection is { } reason)
        {
            return RoundTransition.Rejected(round, reason);
        }

        var answered = round with { Answers = round.Answers.Add(playerId, new QuizAnswer(answer.Choice, receivedAt)) };
        return answered.Phase == QuizPhase.Answering && EverybodyAnswered(answered)
            ? new(answered with { Phase = QuizPhase.Locked }, [new CancelTimer(AnswersTimer)])
            : new(answered, []);
    }

    /// <summary>
    /// Whether the answers of the question in progress opened, with its first choice shown, even if they are locked since.
    /// </summary>
    private static bool AnswersOpened(QuizRound round) => round.Phase != QuizPhase.Presentation || round.ShownChoiceCount > 0;

    /// <summary>
    /// Whether the players may answer the question in progress: from its first choice shown until the answers lock.
    /// </summary>
    private static bool AnswersOpen(QuizRound round) =>
        round.Phase == QuizPhase.Answering || (round.Phase == QuizPhase.Presentation && round.ShownChoiceCount > 0);

    /// <summary>
    /// Whether every participant answered the question in progress: nobody is left to wait for.
    /// </summary>
    private static bool EverybodyAnswered(QuizRound round) => round.Answers.Count == round.Participants.Length;

    /// <summary>
    /// The choices of the question in progress in the order shown, each with its letter.
    /// </summary>
    private static IEnumerable<(QuizChoiceLetter Letter, QuizChoice Choice)> ShownChoicesOf(QuizRound round) =>
        round.ChoiceOrder.Select((index, position) => ((QuizChoiceLetter)position, round.Question.Choices[index]));

    /// <summary>
    /// The letter of the correct choice of the question in progress, as shown on every screen.
    /// </summary>
    private static QuizChoiceLetter CorrectLetterOf(QuizRound round) =>
        ShownChoicesOf(round).First(shown => shown.Choice.Correct).Letter;

    /// <summary>
    /// What the reveal tells a player: nothing to one who did not take part in the question.
    /// </summary>
    private static QuizVerdict? VerdictOf(QuizRound round, PlayerId playerId) =>
        !round.Participants.Contains(playerId) ? null
        : !round.Answers.TryGetValue(playerId, out var answer) ? QuizVerdict.NoAnswer
        : answer.Choice == CorrectLetterOf(round) ? QuizVerdict.Correct
        : QuizVerdict.Wrong;

    /// <summary>
    /// What a participant earned with the question in progress, once revealed: nothing to one who did not take part.
    /// </summary>
    private static int? PointsOf(QuizRound round, PlayerId playerId) =>
        round.Phase == QuizPhase.Revealed && round.Points.TryGetValue(playerId, out var points) ? points : null;

    /// <summary>
    /// The players taking part in the question in progress, in order of arrival, each with their choice. Players are
    /// never removed: every participant is still registered, under their current nickname.
    /// </summary>
    private static IEnumerable<(Player Player, QuizChoiceLetter? Choice)> ParticipantsOf(QuizRound round, GameState game) =>
        game.Players
            .Where(player => round.Participants.Contains(player.Id))
            .Select(player => (player, round.Answers.TryGetValue(player.Id, out var answer) ? answer.Choice : (QuizChoiceLetter?)null));

    /// <summary>
    /// The choices of the question in progress the TV screen shows so far: nothing tells the correct one.
    /// </summary>
    private static ImmutableArray<QuizChoiceView> PublicChoicesOf(QuizRound round) =>
        [.. ShownChoicesOf(round).Take(round.ShownChoiceCount).Select(shown => new QuizChoiceView(shown.Letter, shown.Choice.Text))];

    /// <summary>
    /// When the answers close, while their countdown runs: the screens count down to it.
    /// </summary>
    private static long? CloseTimeOf(QuizRound round) =>
        round.Phase == QuizPhase.Answering ? round.AnswersCloseAt?.ToUnixTimeMilliseconds() : null;

    private static QuizQuestionPhase PhaseOf(QuizRound round) => round.Phase switch
    {
        QuizPhase.Presentation => QuizQuestionPhase.Presentation,
        QuizPhase.Answering => QuizQuestionPhase.Answering,
        QuizPhase.Locked => QuizQuestionPhase.Locked,
        QuizPhase.Revealed => QuizQuestionPhase.Revealed,
        _ => throw new InvalidOperationException($"Phase {round.Phase} has no projection."),
    };

    private static PackProblem Problem(PackProblemCode code, string path, ImmutableDictionary<string, string>? parameters = null) =>
        new(code, PackDescriptor.FileName, path, parameters ?? ImmutableDictionary<string, string>.Empty);
}
