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
/// For now, the mode only checks the consistency of its activities, so that the packs can be loaded and validated. The
/// questions are played from US-E08-02: until then, a quiz round finishes as soon as it starts, and accepts no intent.
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

    /// <inheritdoc />
    public override RoundTransition Start(QuizRoundDescriptor descriptor, GameState game, GameContext context) =>
        new(new QuizRound(), []) { IsFinished = true };

    /// <inheritdoc />
    /// <remarks>Never called: the round finishes as soon as it starts, so the engine rejects every input aimed at it.</remarks>
    public override RoundTransition Handle(QuizRound round, GameInput input, GameState game, GameContext context) =>
        RoundTransition.Rejected(round, RejectionReason.NotInRound);

    /// <inheritdoc />
    public override PlayerRoundView ProjectForPlayer(QuizRound round, GameState game, Player player) => new QuizPlayerView();

    /// <inheritdoc />
    public override DisplayRoundView ProjectForDisplay(QuizRound round, GameState game) => new QuizDisplayView();

    /// <inheritdoc />
    public override GameMasterRoundView ProjectForGameMaster(QuizRound round, GameState game) => new QuizGameMasterView();

    private static PackProblem Problem(PackProblemCode code, string path, ImmutableDictionary<string, string>? parameters = null) =>
        new(code, PackDescriptor.FileName, path, parameters ?? ImmutableDictionary<string, string>.Empty);
}
