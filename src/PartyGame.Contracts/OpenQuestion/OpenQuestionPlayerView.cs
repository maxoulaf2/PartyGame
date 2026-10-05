namespace PartyGame.Contracts.OpenQuestion;

/// <summary>
/// What a round of open questions shows on the phone of one player: a field to type their answer in. Neither the question,
/// read on the TV screen, nor its expected answer before the reveal, nor what the other players answered.
/// </summary>
/// <param name="QuestionNumber">Number of the question in the round, from 1.</param>
/// <param name="QuestionCount">Number of questions of the round.</param>
/// <param name="Phase">Phase of the question.</param>
/// <param name="Numeric">Whether the answer is a number: the field opens a numeric keyboard.</param>
/// <param name="MaxLength">The longest answer accepted, in characters: the field takes no more.</param>
/// <param name="AnswersCloseAt">
/// When the answers close, in milliseconds since the Unix epoch on the clock of the server, while their countdown runs;
/// <see langword="null"/> otherwise.
/// </param>
/// <param name="Participating">
/// Whether the player takes part in the question: always before the question shows, then only if they were registered
/// when it showed, opening the answers. A player who joined meanwhile plays from the next question.
/// </param>
/// <param name="Answer">
/// The answer this player sent, as typed, or <see langword="null"/> while they have not answered.
/// </param>
/// <param name="ExpectedAnswer">
/// The expected answer, as the pack writes it, once revealed; <see langword="null"/> before.
/// </param>
/// <param name="Verdict">
/// Whether the player got it right, once revealed, if they took part in the question; <see langword="null"/> otherwise.
/// </param>
/// <param name="Points">
/// The points the player earned with the question, 0 included, once revealed, if they took part in it;
/// <see langword="null"/> otherwise. Their total is <see cref="PlayerSnapshot.Score"/>.
/// </param>
public sealed record OpenQuestionPlayerView(
    int QuestionNumber,
    int QuestionCount,
    OpenQuestionQuestionPhase Phase,
    bool Numeric,
    int MaxLength,
    long? AnswersCloseAt,
    bool Participating,
    string? Answer,
    string? ExpectedAnswer,
    OpenQuestionVerdict? Verdict,
    int? Points) : PlayerRoundView;
