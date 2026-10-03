using System.Collections.Immutable;

namespace PartyGame.Contracts.Quiz;

/// <summary>
/// What a quiz round shows on the game master console: the question in progress, its correct answer and what each player
/// chose, so that the game master can drive the round.
/// </summary>
/// <param name="QuestionNumber">Number of the question in the round, from 1.</param>
/// <param name="QuestionCount">Number of questions of the round.</param>
/// <param name="Phase">Phase of the question.</param>
/// <param name="Text">The text of the question.</param>
/// <param name="Choices">The choices, in the order shown on every screen, the correct one marked.</param>
/// <param name="AnswersCloseAt">
/// When the answers close, in milliseconds since the Unix epoch on the clock of the server, while they are open;
/// <see langword="null"/> otherwise.
/// </param>
/// <param name="Answers">
/// The players taking part in the question, in order of arrival, each with their choice: empty until the answers open.
/// </param>
public sealed record QuizGameMasterView(
    int QuestionNumber,
    int QuestionCount,
    QuizQuestionPhase Phase,
    string Text,
    ImmutableArray<QuizGameMasterChoice> Choices,
    long? AnswersCloseAt,
    ImmutableArray<QuizGameMasterAnswer> Answers) : GameMasterRoundView;
