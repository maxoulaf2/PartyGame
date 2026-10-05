using System.Collections.Immutable;

namespace PartyGame.Contracts.OpenQuestion;

/// <summary>
/// What a round of open questions shows on the game master console: the question in progress, its expected answer and
/// what each player answered, as they come.
/// </summary>
/// <param name="QuestionNumber">Number of the question in the round, from 1.</param>
/// <param name="QuestionCount">Number of questions of the round.</param>
/// <param name="Phase">Phase of the question.</param>
/// <param name="Text">The text of the question, shown to the game master before the TV screen, so that they read it out.</param>
/// <param name="QuestionShown">Whether the TV screen shows the question, which opens the answers.</param>
/// <param name="ExpectedAnswer">The expected answer, as the pack writes it.</param>
/// <param name="AcceptedAnswers">The other answers the pack accepts, as it writes them.</param>
/// <param name="AnswersCloseAt">
/// When the answers close, in milliseconds since the Unix epoch on the clock of the server, while their countdown runs;
/// <see langword="null"/> otherwise.
/// </param>
/// <param name="Answers">
/// The players taking part in the question, in order of arrival, each with their answer: empty until the answers open.
/// </param>
public sealed record OpenQuestionGameMasterView(
    int QuestionNumber,
    int QuestionCount,
    OpenQuestionQuestionPhase Phase,
    string Text,
    bool QuestionShown,
    string ExpectedAnswer,
    ImmutableArray<string> AcceptedAnswers,
    long? AnswersCloseAt,
    ImmutableArray<OpenQuestionGameMasterAnswer> Answers) : GameMasterRoundView;
