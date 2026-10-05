using System.Collections.Immutable;

namespace PartyGame.Contracts.OpenQuestion;

/// <summary>
/// The answers to the question in progress that are identical once normalized, as the game master console lists them to
/// judge them in one go.
/// </summary>
/// <param name="Text">The answer as typed by most of its authors, or by the first of them on a tie.</param>
/// <param name="Category">How the server pre-classifies the answer.</param>
/// <param name="PlayerIds">The players who gave the answer, in order of arrival in the game.</param>
/// <param name="Accepted">
/// Whether the game master accepted the answer, once they judged the question; <see langword="null"/> before.
/// </param>
public sealed record OpenQuestionGameMasterGroup(
    string Text,
    OpenQuestionAnswerCategory Category,
    ImmutableArray<PlayerId> PlayerIds,
    bool? Accepted);
