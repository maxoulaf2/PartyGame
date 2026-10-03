using System.Collections.Immutable;

namespace PartyGame.Contracts.Quiz;

/// <summary>
/// What a quiz round shows on the phone of one player: an answer pad, one button per choice with its letter, its shape
/// and its color. Neither the question nor the texts of the choices, read on the TV screen, so that players look up from
/// their phones; nor its correct answer before the reveal.
/// </summary>
/// <param name="QuestionNumber">Number of the question in the round, from 1.</param>
/// <param name="QuestionCount">Number of questions of the round.</param>
/// <param name="Phase">Phase of the question.</param>
/// <param name="Choices">The letters of the choices, in the order shown on the TV screen.</param>
public sealed record QuizPlayerView(
    int QuestionNumber,
    int QuestionCount,
    QuizQuestionPhase Phase,
    ImmutableArray<QuizChoiceLetter> Choices) : PlayerRoundView;
