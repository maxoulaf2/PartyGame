using System.Collections.Immutable;
using System.Globalization;
using PartyGame.Contracts;
using PartyGame.Contracts.OpenQuestion;
using PartyGame.Contracts.Packs;
using PartyGame.Engine.Inputs;

namespace PartyGame.Engine.Modes.OpenQuestion;

/// <summary>
/// Plays the rounds of open questions of the packs.
/// </summary>
/// <remarks>
/// For now, the mode only lets the packs be loaded and validated. The questions are played from US-E16-02: until then, a
/// round finishes as soon as it starts, and accepts no intent.
/// </remarks>
public sealed class OpenQuestionMode : GameMode<OpenQuestionRoundDescriptor, OpenQuestionRound>
{
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

    /// <inheritdoc />
    public override RoundTransition Start(OpenQuestionRoundDescriptor descriptor, GameState game, GameContext context) =>
        new(new OpenQuestionRound(), []) { IsFinished = true };

    /// <inheritdoc />
    /// <remarks>Never called: the round finishes as soon as it starts, so the engine rejects every input aimed at it.</remarks>
    public override RoundTransition Handle(OpenQuestionRound round, GameInput input, GameState game, GameContext context) =>
        RoundTransition.Rejected(round, RejectionReason.NotInRound);

    /// <inheritdoc />
    /// <remarks>Never called: a finished round is never resumed.</remarks>
    public override RoundTransition ResumeRound(OpenQuestionRound round, GameState game, TimeSpan shift, GameContext context) =>
        new(round, []);

    /// <inheritdoc />
    public override PlayerRoundView ProjectForPlayer(OpenQuestionRound round, GameState game, Player player) => new OpenQuestionPlayerView();

    /// <inheritdoc />
    public override DisplayRoundView ProjectForDisplay(OpenQuestionRound round, GameState game) => new OpenQuestionDisplayView();

    /// <inheritdoc />
    public override GameMasterRoundView ProjectForGameMaster(OpenQuestionRound round, GameState game) => new OpenQuestionGameMasterView();

    private static PackProblem Problem(PackProblemCode code, string path, string name, string value) =>
        new(code, PackDescriptor.FileName, path, ImmutableDictionary<string, string>.Empty.Add(name, value));
}
