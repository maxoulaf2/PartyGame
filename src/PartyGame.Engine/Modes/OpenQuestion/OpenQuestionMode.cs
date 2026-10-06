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
    /// Plays the intents of the round, each aimed at the question it names. The answers open while the game master reads the
    /// question, before it shows, lock
    /// once every participant answered, or when their timer elapses, then the game master judges them and reveals them.
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
            GameMasterRoundInput { RoundIntent: OpenQuestionJudge judge } => Judge(round, judge),
            GameMasterRoundInput { RoundIntent: OpenQuestionRevealAnswer reveal } => RevealAnswer(round, reveal),
            GameMasterRoundInput { RoundIntent: OpenQuestionNextQuestion next } => NextQuestion(round, next),
            PlayerRoundInput { RoundIntent: OpenQuestionSubmitAnswer answer } submitted =>
                SubmitAnswer(round, game, submitted.PlayerId, answer, submitted.ReceivedAt),
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

            // Whoever is registered before the question shows takes part, and may already answer.
            round.Phase == OpenQuestionPhase.Presentation || round.Participants.Contains(player.Id),
            round.Answers.TryGetValue(player.Id, out var answer) ? answer.Text : null,
            round.Phase == OpenQuestionPhase.Revealed ? round.Question.Answer : null,
            round.Phase == OpenQuestionPhase.Revealed ? VerdictOf(round, player.Id) : null,
            PointsOf(round, player.Id));
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

            // How many answered, never what, once the question shows.
            shown ? round.Answers.Count : 0,
            round.Participants.Length,

            // Once revealed, the answers tell the rest.
            shown && round.Phase != OpenQuestionPhase.Revealed ? ParticipantsOf(round, game) : [],
            round.Phase == OpenQuestionPhase.Revealed ? RevealOf(round, game) : null);
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
                        round.Answers.TryGetValue(player.Id, out var answer) ? answer.Text : null,
                        PointsOf(round, player.Id))),
            ],
            [
                .. round.Groups.Select(group => new OpenQuestionGameMasterGroup(
                    group.Text,
                    group.Category,
                    group.Players,
                    round.Phase is OpenQuestionPhase.Judged or OpenQuestionPhase.Revealed ? group.Players.All(round.AcceptedPlayers.Contains) : null)),
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
    /// One step per question.
    /// </summary>
    /// <inheritdoc />
    public override int CountPreviewSteps(OpenQuestionRoundDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        return descriptor.Questions.Length;
    }

    /// <summary>
    /// The question revealed with its answer, without any answer of a player.
    /// </summary>
    /// <inheritdoc />
    public override RoundPreview Preview(OpenQuestionRoundDescriptor descriptor, int stepIndex, DateTimeOffset? excerptStartsAt) =>
        new(new OpenQuestionRound(descriptor, stepIndex, OpenQuestionPhase.Revealed), HasExcerpt: false);

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
    /// Shows the question presented on the TV screen, with its image: the players registered now take part, and the
    /// countdown starts, unless they all answered while the question was read.
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
        var closeAt = context.Now + AnswerTimeOf(round);
        var shown = round with { Participants = [.. game.Players.Select(player => player.Id)], AnswersCloseAt = closeAt };
        return shown.Answers.Count == shown.Participants.Length
            ? new(Lock(shown), [])
            : new(shown with { Phase = OpenQuestionPhase.Answering }, [new ScheduleTimer(AnswersTimer, closeAt)]);
    }

    /// <summary>
    /// Gives up the question in progress before its reveal: its answers are ignored, and its countdown stops if it runs.
    /// Once revealed, the game master moves on to the next question instead.
    /// </summary>
    private static RoundTransition SkipQuestion(OpenQuestionRound round, OpenQuestionSkipQuestion skip)
    {
        RejectionReason? rejection =
            skip.QuestionNumber != round.QuestionNumber ? RejectionReason.QuestionMismatch
            : round.Phase == OpenQuestionPhase.Revealed ? RejectionReason.PhaseMismatch
            : null;
        if (rejection is { } reason)
        {
            return RoundTransition.Rejected(round, reason);
        }

        var skipped = round with { SkippedQuestions = round.SkippedQuestions.Add(round.QuestionIndex) };
        return MoveOn(skipped, round.Phase == OpenQuestionPhase.Answering ? [new CancelTimer(AnswersTimer)] : []);
    }

    /// <summary>
    /// Reveals the question judged: the expected answer and every answer received. The points of the question are awarded
    /// now, never before, so that no score tells a verdict ahead of time.
    /// </summary>
    private static RoundTransition RevealAnswer(OpenQuestionRound round, OpenQuestionRevealAnswer reveal)
    {
        RejectionReason? rejection =
            reveal.QuestionNumber != round.QuestionNumber ? RejectionReason.QuestionMismatch
            : round.Phase != OpenQuestionPhase.Judged ? RejectionReason.PhaseMismatch
            : null;
        if (rejection is { } reason)
        {
            return RoundTransition.Rejected(round, reason);
        }

        var points = round.Participants.ToImmutableDictionary(
            player => player,
            player => round.AcceptedPlayers.Contains(player) ? PointsFor(round, round.Answers[player]) : 0);
        return new(round with { Phase = OpenQuestionPhase.Revealed, Points = points }, []) { Points = points };
    }

    /// <summary>
    /// The points of an accepted answer: those of the round, plus its speed bonus in proportion to the time left when the
    /// answer was received, rounded to the nearest integer. Computed in integers, so that the result is exact and
    /// reproducible.
    /// </summary>
    private static int PointsFor(OpenQuestionRound round, OpenAnswer answer)
    {
        var duration = AnswerTimeOf(round).Ticks;

        // Set when the question showed, which opened the answers: any answer has one.
        var left = Math.Clamp((round.AnswersCloseAt!.Value - answer.ReceivedAt).Ticks, 0, duration);
        var bonus = ((2 * round.Descriptor.SpeedBonus * left) + duration) / (2 * duration);
        return round.Descriptor.Points + (int)bonus;
    }

    /// <summary>
    /// Moves on from the revealed question: to the next one, or to the end of the round after the last one.
    /// </summary>
    private static RoundTransition NextQuestion(OpenQuestionRound round, OpenQuestionNextQuestion next)
    {
        RejectionReason? rejection =
            next.QuestionNumber != round.QuestionNumber ? RejectionReason.QuestionMismatch
            : round.Phase != OpenQuestionPhase.Revealed ? RejectionReason.PhaseMismatch
            : null;
        return rejection is { } reason ? RoundTransition.Rejected(round, reason) : MoveOn(round, []);
    }

    /// <summary>
    /// Presents the question that follows the one in progress, or ends the round after its last one, the round then
    /// staying on it.
    /// </summary>
    private static RoundTransition MoveOn(OpenQuestionRound round, ImmutableArray<Effect> effects)
    {
        if (round.QuestionIndex == round.Descriptor.Questions.Length - 1)
        {
            return new(round, effects) { IsFinished = true };
        }

        var next = new OpenQuestionRound(round.Descriptor, round.QuestionIndex + 1, OpenQuestionPhase.Presentation)
        {
            SkippedQuestions = round.SkippedQuestions,
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
            : new(Lock(round), []);

    /// <summary>
    /// Records the first answer of a participant, received before the answers close, as typed: never truncated, rejected
    /// when too long or empty once normalized. The answers open while the game master reads the question, before it shows:
    /// any registered player may answer then. Locks the answers once every participant answered: nobody is left to wait
    /// for.
    /// </summary>
    private static RoundTransition SubmitAnswer(OpenQuestionRound round, GameState game, PlayerId playerId, OpenQuestionSubmitAnswer answer, DateTimeOffset receivedAt)
    {
        var presented = round.Phase == OpenQuestionPhase.Presentation;
        RejectionReason? rejection =
            answer.QuestionNumber != round.QuestionNumber ? RejectionReason.QuestionMismatch
            : !presented && round.Phase != OpenQuestionPhase.Answering ? RejectionReason.PhaseMismatch

            // Received once closed, although the loop has not handled the timer yet: the time of reception decides.
            : receivedAt >= round.AnswersCloseAt ? RejectionReason.AnswerTooLate

            // Before the question shows, its participants are not fixed yet: whoever is registered then takes part.
            : !(presented ? game.Players.Any(player => player.Id == playerId) : round.Participants.Contains(playerId)) ? RejectionReason.NotParticipating
            : round.Answers.ContainsKey(playerId) ? RejectionReason.AlreadyAnswered
            : answer.Answer.Length > round.Descriptor.MaxLength ? RejectionReason.AnswerTooLong
            : OpenAnswers.Normalize(answer.Answer).Length == 0 ? RejectionReason.AnswerEmpty
            : null;
        if (rejection is { } reason)
        {
            return RoundTransition.Rejected(round, reason);
        }

        var answered = round with { Answers = round.Answers.Add(playerId, new OpenAnswer(answer.Answer, receivedAt)) };
        return !presented && answered.Answers.Count == answered.Participants.Length
            ? new(Lock(answered), [new CancelTimer(AnswersTimer)])
            : new(answered, []);
    }

    /// <summary>
    /// Locks the answers, grouped by their normalized text and pre-classified for the game master to judge them. Without
    /// any answer, there is nothing to judge: the question is judged at once.
    /// </summary>
    private static OpenQuestionRound Lock(OpenQuestionRound round)
    {
        var groups = round.Participants
            .Where(round.Answers.ContainsKey)
            .GroupBy(player => OpenAnswers.Normalize(round.Answers[player].Text), StringComparer.Ordinal)
            .Select(group => new OpenAnswerGroup(

                // The ordering is stable: on a tie, the text of the first author wins.
                group.GroupBy(player => round.Answers[player].Text, StringComparer.Ordinal).OrderByDescending(texts => texts.Count()).First().Key,
                OpenAnswers.Classify(group.Key, round.Question),
                [.. group]))
            .OrderBy(group => group.Category)
            .ToImmutableArray();
        return round with { Phase = groups.IsEmpty ? OpenQuestionPhase.Judged : OpenQuestionPhase.Locked, Groups = groups };
    }

    /// <summary>
    /// Judges the answers locked in one go: the players named answered right, the other participants wrong. A second
    /// judgment, sent twice or by another console, is obsolete: the first one wins.
    /// </summary>
    private static RoundTransition Judge(OpenQuestionRound round, OpenQuestionJudge judge)
    {
        RejectionReason? rejection =
            judge.QuestionNumber != round.QuestionNumber ? RejectionReason.QuestionMismatch
            : round.Phase != OpenQuestionPhase.Locked ? RejectionReason.PhaseMismatch
            : !judge.AcceptedPlayers.All(round.Answers.ContainsKey) ? RejectionReason.PlayerWithoutAnswer
            : null;
        return rejection is { } reason
            ? RoundTransition.Rejected(round, reason)
            : new(round with { Phase = OpenQuestionPhase.Judged, AcceptedPlayers = [.. round.Participants.Where(judge.AcceptedPlayers.Contains)] }, []);
    }

    /// <summary>
    /// What the reveal tells a player: nothing to one who did not take part in the question.
    /// </summary>
    private static OpenQuestionVerdict? VerdictOf(OpenQuestionRound round, PlayerId playerId) =>
        !round.Participants.Contains(playerId) ? null
        : !round.Answers.ContainsKey(playerId) ? OpenQuestionVerdict.NoAnswer
        : round.AcceptedPlayers.Contains(playerId) ? OpenQuestionVerdict.Correct
        : OpenQuestionVerdict.Wrong;

    /// <summary>
    /// What a participant earned with the question in progress, once revealed: nothing to one who did not take part.
    /// </summary>
    private static int? PointsOf(OpenQuestionRound round, PlayerId playerId) =>
        round.Phase == OpenQuestionPhase.Revealed && round.Points.TryGetValue(playerId, out var points) ? points : null;

    /// <summary>
    /// What the TV screen shows of the question revealed: each group of answers with its authors, the correct ones first,
    /// then the participants without answer. A group judged partly, which the console never sends, splits into its
    /// correct and wrong authors, so that the screen always agrees with the points.
    /// </summary>
    private static OpenQuestionDisplayReveal RevealOf(OpenQuestionRound round, GameState game)
    {
        // Players are never removed: every participant is still registered, under their current nickname.
        string NicknameOf(PlayerId id) => game.Players.First(player => player.Id == id).Nickname;

        var groups = round.Groups
            .SelectMany(group => group.Players
                .GroupBy(round.AcceptedPlayers.Contains)
                .Select(authors => new OpenQuestionRevealedGroup(group.Text, authors.Key, [.. authors.Select(NicknameOf)])))
            .OrderByDescending(group => group.Correct);
        return new OpenQuestionDisplayReveal(
            round.Question.Answer,
            [.. groups],
            [.. round.Participants.Where(player => !round.Answers.ContainsKey(player)).Select(NicknameOf)]);
    }

    /// <summary>
    /// The participants as the TV screen lists them, each with the time they took to answer since the question showed: no
    /// less than 0 for an answer sent while it was read.
    /// </summary>
    private static ImmutableArray<OpenQuestionDisplayParticipant> ParticipantsOf(OpenQuestionRound round, GameState game)
    {
        // Set when the question showed, as the countdown started.
        var shownAt = round.AnswersCloseAt!.Value - AnswerTimeOf(round);
        return
        [
            .. game.Players
                .Where(player => round.Participants.Contains(player.Id))
                .Select(player => new OpenQuestionDisplayParticipant(
                    player.Nickname,
                    round.Answers.TryGetValue(player.Id, out var answer) ? Math.Max(0, (long)(answer.ReceivedAt - shownAt).TotalMilliseconds) : null)),
        ];
    }

    /// <summary>
    /// The time given to answer the question in progress.
    /// </summary>
    private static TimeSpan AnswerTimeOf(OpenQuestionRound round) =>
        TimeSpan.FromSeconds(round.Question.AnswerSeconds ?? round.Descriptor.AnswerSeconds);

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
        OpenQuestionPhase.Judged => OpenQuestionQuestionPhase.Judged,
        OpenQuestionPhase.Revealed => OpenQuestionQuestionPhase.Revealed,
        _ => throw new InvalidOperationException($"Phase {round.Phase} has no projection."),
    };

    private static PackProblem Problem(PackProblemCode code, string path, string name, string value) =>
        new(code, PackDescriptor.FileName, path, ImmutableDictionary<string, string>.Empty.Add(name, value));
}
