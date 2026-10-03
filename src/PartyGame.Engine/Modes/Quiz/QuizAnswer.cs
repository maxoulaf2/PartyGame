using PartyGame.Contracts.Quiz;

namespace PartyGame.Engine.Modes.Quiz;

/// <summary>
/// The answer of a player to the question in progress: only the first one counts.
/// </summary>
/// <param name="Choice">The letter of the chosen choice, as shown on every screen.</param>
/// <param name="ReceivedAt">
/// Server time at which the hub received the answer, kept for the speed bonus (US-E09-01): what counts is when the
/// answer arrived, not when the loop handled it.
/// </param>
public sealed record QuizAnswer(QuizChoiceLetter Choice, DateTimeOffset ReceivedAt);
