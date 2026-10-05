using System.Collections.Immutable;

namespace PartyGame.Contracts.OpenQuestion;

/// <summary>
/// Answers identical once normalized, as the TV screen reveals them, with their authors.
/// </summary>
/// <param name="Text">The answer as typed by most of its authors.</param>
/// <param name="Correct">Whether the game master accepted it.</param>
/// <param name="Nicknames">The nicknames of its authors, in order of arrival in the game, to show as plain text.</param>
public sealed record OpenQuestionRevealedGroup(string Text, bool Correct, ImmutableArray<string> Nicknames);
