namespace PartyGame.Contracts.OpenQuestion;

/// <summary>
/// What a round of open questions shows on the TV screen: the question once the game master shows it, never its expected
/// answer, nor what any player answered before the reveal: only how many answered.
/// </summary>
/// <param name="QuestionNumber">Number of the question in the round, from 1.</param>
/// <param name="QuestionCount">Number of questions of the round.</param>
/// <param name="Phase">Phase of the question.</param>
/// <param name="Text">The text of the question, once the game master shows it; <see langword="null"/> before.</param>
/// <param name="ImageUrl">
/// The URL of the image of the question, shown with its text; <see langword="null"/> before, or when it has none.
/// </param>
/// <param name="AnswersCloseAt">
/// When the answers close, in milliseconds since the Unix epoch on the clock of the server, while their countdown runs;
/// <see langword="null"/> otherwise.
/// </param>
/// <param name="AnsweredCount">How many players taking part answered: 0 until the answers open.</param>
/// <param name="ParticipantCount">
/// How many players take part in the question, the players registered when the question showed, connected or not: 0
/// until then.
/// </param>
/// <param name="Reveal">The expected answer and every answer received, once revealed; <see langword="null"/> before.</param>
public sealed record OpenQuestionDisplayView(
    int QuestionNumber,
    int QuestionCount,
    OpenQuestionQuestionPhase Phase,
    string? Text,
    string? ImageUrl,
    long? AnswersCloseAt,
    int AnsweredCount,
    int ParticipantCount,
    OpenQuestionDisplayReveal? Reveal) : DisplayRoundView;
