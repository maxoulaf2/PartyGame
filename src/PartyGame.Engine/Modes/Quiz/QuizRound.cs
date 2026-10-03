using System.Collections.Immutable;
using System.Text.Json.Serialization;
using PartyGame.Contracts.Packs;

namespace PartyGame.Engine.Modes.Quiz;

/// <summary>
/// State of a quiz round: the question in progress, its phase and the order its choices are shown in.
/// </summary>
/// <param name="Descriptor">The activity of the pack the round plays, for its questions.</param>
/// <param name="QuestionIndex">Position of the question in progress in <paramref name="Descriptor"/>, from 0.</param>
/// <param name="Phase">Phase of the question in progress.</param>
/// <param name="ChoiceOrder">
/// The position in the descriptor of each choice of the question in progress, in the order shown: the first one is
/// choice A. Drawn when the question is presented, so that every screen shows the same order.
/// </param>
public sealed record QuizRound(QuizRoundDescriptor Descriptor, int QuestionIndex, QuizPhase Phase, ImmutableArray<int> ChoiceOrder)
    : RoundState
{
    /// <summary>
    /// The question in progress.
    /// </summary>
    [JsonIgnore] // read from the descriptor, which is persisted
    public QuizQuestion Question => Descriptor.Questions[QuestionIndex];
}
