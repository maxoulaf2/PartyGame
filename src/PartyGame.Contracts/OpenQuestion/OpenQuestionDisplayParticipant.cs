namespace PartyGame.Contracts.OpenQuestion;

/// <summary>
/// A player taking part in the open question shown, as the TV screen lists them: whether they answered and how fast,
/// never what.
/// </summary>
/// <param name="Nickname">Their nickname, to show as plain text.</param>
/// <param name="AnswerMilliseconds">
/// How long they took to answer once the question showed, in milliseconds, 0 when they answered while it was read;
/// <see langword="null"/> until they answer.
/// </param>
public sealed record OpenQuestionDisplayParticipant(string Nickname, long? AnswerMilliseconds);
