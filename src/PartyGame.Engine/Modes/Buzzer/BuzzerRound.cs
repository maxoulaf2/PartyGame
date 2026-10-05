using System.Text.Json.Serialization;
using PartyGame.Contracts.Packs;

namespace PartyGame.Engine.Modes.Buzzer;

/// <summary>
/// State of a round of buzzer questions: the question in progress and its buzzer.
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
    /// Phase of the question in progress.
    /// </summary>
    [JsonIgnore] // derived from the buzzer, which is persisted
    public BuzzerPhase Phase =>
        Buzzer.Opening == 0 ? BuzzerPhase.Ready
        : Buzzer.Winner is not null ? BuzzerPhase.Answering
        : Buzzer.ArbitrateAt is not null ? BuzzerPhase.Arbitrating
        : BuzzerPhase.Open;
}
