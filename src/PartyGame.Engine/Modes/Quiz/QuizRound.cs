using System.Collections.Immutable;
using System.Text.Json.Serialization;
using PartyGame.Contracts;
using PartyGame.Contracts.Packs;

namespace PartyGame.Engine.Modes.Quiz;

/// <summary>
/// State of a quiz round: the question in progress, its phase, the order its choices are shown in, and its answers.
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

    /// <summary>
    /// The number of the question in progress, from 1, as the screens show it and the intents name it.
    /// </summary>
    [JsonIgnore] // read from the index, which is persisted
    public int QuestionNumber => QuestionIndex + 1;

    /// <summary>
    /// When the answers of the question in progress close, set when they open, or <see langword="null"/> before. Kept once
    /// they are locked, even early, for the speed bonus.
    /// </summary>
    public DateTimeOffset? AnswersCloseAt { get; init; }

    /// <summary>
    /// The players taking part in the question in progress: those registered when its answers opened, connected or not,
    /// in order of arrival. Empty before. A player who joins later plays from the next question.
    /// </summary>
    public ImmutableArray<PlayerId> Participants { get; init; } = [];

    /// <summary>
    /// The answer of each participant who answered the question in progress: their first one only.
    /// </summary>
    public ImmutableDictionary<PlayerId, QuizAnswer> Answers { get; init; } = ImmutableDictionary<PlayerId, QuizAnswer>.Empty;

    /// <summary>
    /// The positions in <see cref="Descriptor"/> of the questions the game master skipped before their reveal, in order,
    /// kept for the history of the round: they score nothing.
    /// </summary>
    public ImmutableArray<int> SkippedQuestions { get; init; } = [];
}
