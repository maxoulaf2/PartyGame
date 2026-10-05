using System.Collections.Immutable;

namespace PartyGame.Contracts.OpenQuestion;

/// <summary>
/// What the TV screen shows once an open question is revealed: the expected answer, then every answer received.
/// </summary>
/// <param name="ExpectedAnswer">The expected answer, as the pack writes it.</param>
/// <param name="Groups">The answers received, grouped, the correct ones first, then the wrong ones.</param>
/// <param name="WithoutAnswer">The nicknames of the participants who did not answer, in order of arrival.</param>
public sealed record OpenQuestionDisplayReveal(
    string ExpectedAnswer,
    ImmutableArray<OpenQuestionRevealedGroup> Groups,
    ImmutableArray<string> WithoutAnswer);
