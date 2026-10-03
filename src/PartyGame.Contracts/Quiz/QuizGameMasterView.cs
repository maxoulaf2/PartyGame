using System.Collections.Immutable;

namespace PartyGame.Contracts.Quiz;

/// <summary>
/// What a quiz round shows on the game master console: the question in progress and its correct answer, so that the game
/// master can drive the round.
/// </summary>
/// <param name="QuestionNumber">Number of the question in the round, from 1.</param>
/// <param name="QuestionCount">Number of questions of the round.</param>
/// <param name="Phase">Phase of the question.</param>
/// <param name="Text">The text of the question.</param>
/// <param name="Choices">The choices, in the order shown on every screen, the correct one marked.</param>
public sealed record QuizGameMasterView(
    int QuestionNumber,
    int QuestionCount,
    QuizQuestionPhase Phase,
    string Text,
    ImmutableArray<QuizGameMasterChoice> Choices) : GameMasterRoundView;
