using System.Collections.Immutable;

namespace PartyGame.Contracts.Quiz;

/// <summary>
/// What a quiz round shows on the TV screen: the question in progress, never its correct answer before the reveal.
/// </summary>
/// <param name="QuestionNumber">Number of the question in the round, from 1.</param>
/// <param name="QuestionCount">Number of questions of the round.</param>
/// <param name="Phase">Phase of the question.</param>
/// <param name="Text">The text of the question.</param>
/// <param name="ImageUrl">
/// The URL of the image of the question, or <see langword="null"/> when it has none. Only the TV screen shows it.
/// </param>
/// <param name="Choices">The choices, in the order shown, which is the order of their letters.</param>
public sealed record QuizDisplayView(
    int QuestionNumber,
    int QuestionCount,
    QuizQuestionPhase Phase,
    string Text,
    string? ImageUrl,
    ImmutableArray<QuizChoiceView> Choices) : DisplayRoundView;
