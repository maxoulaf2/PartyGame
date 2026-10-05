using System.Text.Json.Serialization;
using PartyGame.Contracts;
using PartyGame.Contracts.Packs;

namespace PartyGame.Engine.Modes.Buzzer;

/// <summary>
/// State of a round of buzzer questions: the question in progress, its buzzer, and its reveal.
/// </summary>
/// <param name="Descriptor">The activity of the pack the round plays, for its questions.</param>
/// <param name="QuestionIndex">Position of the question in progress in <paramref name="Descriptor"/>, from 0.</param>
public sealed record BuzzerRound(BuzzerRoundDescriptor Descriptor, int QuestionIndex) : RoundState
{
    /// <summary>
    /// The question in progress.
    /// </summary>
    [JsonIgnore] // read from the descriptor, which is persisted
    public BuzzerQuestion Question => Descriptor.Questions[QuestionIndex];

    /// <summary>
    /// The number of the question in progress, from 1, as the screens show it and the intents name it.
    /// </summary>
    [JsonIgnore] // read from the index, which is persisted
    public int QuestionNumber => QuestionIndex + 1;

    /// <summary>
    /// The buzzer of the question in progress: closed until the game master asks it, a fresh one for each question.
    /// </summary>
    public Buzzers.Buzzer Buzzer { get; init; } = new();

    /// <summary>
    /// Whether the expected answer of the question in progress is revealed.
    /// </summary>
    public bool Revealed { get; init; }

    /// <summary>
    /// The player whose answer to the question in progress was judged correct, or <see langword="null"/>: they win the
    /// points of the round.
    /// </summary>
    public PlayerId? FoundBy { get; init; }

    /// <summary>
    /// Phase of the question in progress.
    /// </summary>
    [JsonIgnore] // derived from the buzzer and the reveal, which are persisted
    public BuzzerPhase Phase =>
        Revealed ? BuzzerPhase.Revealed
        : Buzzer.Opening == 0 ? BuzzerPhase.Ready
        : Buzzer.IsClosed ? BuzzerPhase.Closed
        : Buzzer.Winner is not null ? BuzzerPhase.Answering
        : Buzzer.ArbitrateAt is not null ? BuzzerPhase.Arbitrating
        : BuzzerPhase.Open;
}
