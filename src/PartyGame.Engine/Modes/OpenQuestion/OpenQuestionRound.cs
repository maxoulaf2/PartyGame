using System.Collections.Immutable;
using System.Text.Json.Serialization;
using PartyGame.Contracts;
using PartyGame.Contracts.Packs;

namespace PartyGame.Engine.Modes.OpenQuestion;

/// <summary>
/// State of a round of open questions: the question in progress, its phase and its answers.
/// </summary>
/// <param name="Descriptor">The activity of the pack the round plays, for its questions.</param>
/// <param name="QuestionIndex">Position of the question in progress in <paramref name="Descriptor"/>, from 0.</param>
/// <param name="Phase">Phase of the question in progress.</param>
public sealed record OpenQuestionRound(OpenQuestionRoundDescriptor Descriptor, int QuestionIndex, OpenQuestionPhase Phase) : RoundState
{
    /// <summary>
    /// The question in progress.
    /// </summary>
    [JsonIgnore] // read from the descriptor, which is persisted
    public OpenQuestionDescriptor Question => Descriptor.Questions[QuestionIndex];

    /// <summary>
    /// The number of the question in progress, from 1, as the screens show it and the intents name it.
    /// </summary>
    [JsonIgnore] // read from the index, which is persisted
    public int QuestionNumber => QuestionIndex + 1;

    /// <summary>
    /// When the answers of the question in progress close, set when the question shows, or <see langword="null"/> before.
    /// Kept once they are locked, even early once everybody answered, for the speed bonus.
    /// </summary>
    public DateTimeOffset? AnswersCloseAt { get; init; }

    /// <summary>
    /// The players taking part in the question in progress: those registered when it showed, connected or not, in order of
    /// arrival. Empty before. A player who joins later plays from the next question.
    /// </summary>
    public ImmutableArray<PlayerId> Participants { get; init; } = [];

    /// <summary>
    /// The answer of each participant who answered the question in progress: their first one only.
    /// </summary>
    public ImmutableDictionary<PlayerId, OpenAnswer> Answers { get; init; } = ImmutableDictionary<PlayerId, OpenAnswer>.Empty;

    /// <summary>
    /// The answers to the question in progress grouped and pre-classified when they lock, accepted first, then to check,
    /// then rejected: kept so that a console reloaded, or a game resumed, finds the same suggestions. Empty before.
    /// </summary>
    public ImmutableArray<OpenAnswerGroup> Groups { get; init; } = [];

    /// <summary>
    /// The participants whose answer to the question in progress the game master accepted, in the order of the
    /// participants: empty until they judge it.
    /// </summary>
    public ImmutableArray<PlayerId> AcceptedPlayers { get; init; } = [];

    /// <summary>
    /// The positions in <see cref="Descriptor"/> of the questions the game master skipped, in order, kept for the history of
    /// the round: they score nothing.
    /// </summary>
    public ImmutableArray<int> SkippedQuestions { get; init; } = [];
}
