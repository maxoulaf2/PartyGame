using System.Collections.Immutable;

namespace PartyGame.Contracts.Quiz;

/// <summary>
/// What a quiz round shows on the phone of one player: the question in progress, never its correct answer before the
/// reveal. A phone shows no media.
/// </summary>
/// <param name="QuestionNumber">Number of the question in the round, from 1.</param>
/// <param name="QuestionCount">Number of questions of the round.</param>
/// <param name="Phase">Phase of the question.</param>
/// <param name="Text">The text of the question.</param>
/// <param name="Choices">The choices, in the order shown, which is the order of their letters.</param>
public sealed record QuizPlayerView(
    int QuestionNumber,
    int QuestionCount,
    QuizQuestionPhase Phase,
    string Text,
    ImmutableArray<QuizChoiceView> Choices) : PlayerRoundView;
