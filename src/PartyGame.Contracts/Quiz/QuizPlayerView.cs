using System.Collections.Immutable;

namespace PartyGame.Contracts.Quiz;

/// <summary>
/// What a quiz round shows on the phone of one player: an answer pad, one button per choice with its letter, its shape
/// and its color. Neither the question nor the texts of the choices, read on the TV screen, so that players look up from
/// their phones; nor its correct answer before the reveal, nor what the other players chose.
/// </summary>
/// <param name="QuestionNumber">Number of the question in the round, from 1.</param>
/// <param name="QuestionCount">Number of questions of the round.</param>
/// <param name="Phase">Phase of the question.</param>
/// <param name="Choices">The letters of the choices, in the order shown on the TV screen.</param>
/// <param name="AnswersCloseAt">
/// When the answers close, in milliseconds since the Unix epoch on the clock of the server, while they are open;
/// <see langword="null"/> otherwise.
/// </param>
/// <param name="Participating">
/// Whether the player takes part in the question: always during the presentation, then only if they were registered when
/// its answers opened. A player who joined meanwhile plays from the next question.
/// </param>
/// <param name="Answer">The letter this player chose, or <see langword="null"/> while they have not answered.</param>
public sealed record QuizPlayerView(
    int QuestionNumber,
    int QuestionCount,
    QuizQuestionPhase Phase,
    ImmutableArray<QuizChoiceLetter> Choices,
    long? AnswersCloseAt,
    bool Participating,
    QuizChoiceLetter? Answer) : PlayerRoundView;
