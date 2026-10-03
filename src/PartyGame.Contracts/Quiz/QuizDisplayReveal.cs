using System.Collections.Immutable;

namespace PartyGame.Contracts.Quiz;

/// <summary>
/// What the TV screen shows once the answer of a question is revealed: the correct choice and who chose what.
/// </summary>
/// <param name="CorrectChoice">The letter of the correct choice.</param>
/// <param name="Answers">
/// The players who took part in the question, in order of arrival, each with their choice or without answer.
/// </param>
public sealed record QuizDisplayReveal(QuizChoiceLetter CorrectChoice, ImmutableArray<QuizRevealedAnswer> Answers);
