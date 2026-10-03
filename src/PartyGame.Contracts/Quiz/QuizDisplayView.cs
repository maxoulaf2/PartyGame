using System.Collections.Immutable;

namespace PartyGame.Contracts.Quiz;

/// <summary>
/// What a quiz round shows on the TV screen: the question in progress, never its correct answer before the reveal, nor
/// what any player chose: only how many answered. Both show once the answer is revealed. During the presentation, the
/// question and its choices show as the game master reads them out: nothing the TV screen does not show yet is sent.
/// </summary>
/// <param name="QuestionNumber">Number of the question in the round, from 1.</param>
/// <param name="QuestionCount">Number of questions of the round.</param>
/// <param name="Phase">Phase of the question.</param>
/// <param name="Text">
/// The text of the question, once the game master shows it; <see langword="null"/> before.
/// </param>
/// <param name="ImageUrl">
/// The URL of the image of the question, shown with its text; <see langword="null"/> before, or when it has none. Only
/// the TV screen shows it.
/// </param>
/// <param name="Choices">
/// The choices shown so far, in the order shown, which is the order of their letters: they show one by one during the
/// presentation, the countdown starting with the last one.
/// </param>
/// <param name="ChoiceCount">
/// How many choices the question has, shown or not, so that the screen keeps their room: the phones show as many
/// buttons from the start, each unlocked as its choice shows.
/// </param>
/// <param name="AnswersCloseAt">
/// When the answers close, in milliseconds since the Unix epoch on the clock of the server, while their countdown runs;
/// <see langword="null"/> otherwise. The countdown is computed from it and from the offset of the clock.
/// </param>
/// <param name="AnsweredCount">How many players taking part answered: 0 until the answers open, with the first choice.</param>
/// <param name="ParticipantCount">
/// How many players take part in the question, the players registered when its answers opened with its first choice,
/// connected or not: 0 until the answers open.
/// </param>
/// <param name="Reveal">
/// The correct choice and what each player chose, once the answer is revealed; <see langword="null"/> before.
/// </param>
public sealed record QuizDisplayView(
    int QuestionNumber,
    int QuestionCount,
    QuizQuestionPhase Phase,
    string? Text,
    string? ImageUrl,
    ImmutableArray<QuizChoiceView> Choices,
    int ChoiceCount,
    long? AnswersCloseAt,
    int AnsweredCount,
    int ParticipantCount,
    QuizDisplayReveal? Reveal) : DisplayRoundView;
