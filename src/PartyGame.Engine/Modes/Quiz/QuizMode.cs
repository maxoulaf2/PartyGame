using System.Collections.Immutable;
using System.Globalization;
using PartyGame.Contracts;
using PartyGame.Contracts.Packs;
using PartyGame.Contracts.Quiz;
using PartyGame.Engine.Inputs;
using PartyGame.Engine.Text;

namespace PartyGame.Engine.Modes.Quiz;

/// <summary>
/// Plays the multiple-choice quiz rounds of the packs.
/// </summary>
/// <remarks>
/// For now, a round presents its first question and stays there: the answers open from US-E08-03, and the round moves
/// on to its next questions from US-E08-05.
/// </remarks>
public sealed class QuizMode : GameMode<QuizRoundDescriptor, QuizRound>
{
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

    /// <inheritdoc />
    /// <remarks>
    /// The round schedules no timer, and plays no intent yet: the intents of the contracts are placeholders until
    /// US-E08-03.
    /// </remarks>
    public override RoundTransition Handle(QuizRound round, GameInput input, GameState game, GameContext context) =>
        input switch
        {
            TimerElapsed => RoundTransition.Rejected(round, RejectionReason.UnexpectedTimer),
            _ => RoundTransition.Rejected(round, RejectionReason.IntentUnsupported),
        };

    /// <inheritdoc />
    public override PlayerRoundView ProjectForPlayer(QuizRound round, GameState game, Player player)
    {
        ArgumentNullException.ThrowIfNull(round);
        return new QuizPlayerView(
            round.QuestionIndex + 1,
            round.Descriptor.Questions.Length,
            PhaseOf(round),
            round.Question.Text,
            PublicChoicesOf(round));
    }

    /// <inheritdoc />
    public override DisplayRoundView ProjectForDisplay(QuizRound round, GameState game)
    {
        ArgumentNullException.ThrowIfNull(round);
        ArgumentNullException.ThrowIfNull(game);
        return new QuizDisplayView(
            round.QuestionIndex + 1,
            round.Descriptor.Questions.Length,
            PhaseOf(round),
            round.Question.Text,
            round.Question.Image is { } image ? game.Media.UrlOf(image) : null,
            PublicChoicesOf(round));
    }

    /// <inheritdoc />
    public override GameMasterRoundView ProjectForGameMaster(QuizRound round, GameState game)
    {
        ArgumentNullException.ThrowIfNull(round);
        return new QuizGameMasterView(
            round.QuestionIndex + 1,
            round.Descriptor.Questions.Length,
            PhaseOf(round),
            round.Question.Text,
            [.. ShownChoicesOf(round).Select(shown => new QuizGameMasterChoice(shown.Letter, shown.Choice.Text, shown.Choice.Correct))]);
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
    /// The choices of the question in progress in the order shown, each with its letter.
    /// </summary>
    private static IEnumerable<(QuizChoiceLetter Letter, QuizChoice Choice)> ShownChoicesOf(QuizRound round) =>
        round.ChoiceOrder.Select((index, position) => ((QuizChoiceLetter)position, round.Question.Choices[index]));

    /// <summary>
    /// The choices of the question in progress as the TV screen and the phones show them: nothing tells the correct one.
    /// </summary>
    private static ImmutableArray<QuizChoiceView> PublicChoicesOf(QuizRound round) =>
        [.. ShownChoicesOf(round).Select(shown => new QuizChoiceView(shown.Letter, shown.Choice.Text))];

    private static QuizQuestionPhase PhaseOf(QuizRound round) => round.Phase switch
    {
        QuizPhase.Presentation => QuizQuestionPhase.Presentation,
        _ => throw new InvalidOperationException($"Phase {round.Phase} has no projection."),
    };

    private static PackProblem Problem(PackProblemCode code, string path, ImmutableDictionary<string, string>? parameters = null) =>
        new(code, PackDescriptor.FileName, path, parameters ?? ImmutableDictionary<string, string>.Empty);
}
