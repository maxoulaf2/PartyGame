using System.Collections.Immutable;
using PartyGame.Contracts;
using PartyGame.Contracts.OpenQuestion;

namespace PartyGame.Engine.Modes.OpenQuestion;

/// <summary>
/// The answers to the question in progress that are identical once normalized, pre-classified when the answers lock.
/// </summary>
/// <param name="Text">The answer as typed by most of its authors, or by the first of them on a tie.</param>
/// <param name="Category">How the answer compares to the expected answer and its variants.</param>
/// <param name="Players">The players who gave the answer, in the order of the participants.</param>
public sealed record OpenAnswerGroup(string Text, OpenQuestionAnswerCategory Category, ImmutableArray<PlayerId> Players);
