using System.Collections.Immutable;

namespace PartyGame.Contracts.OpenQuestion;

/// <summary>
/// The game master judges every answer to the question in progress in one go, once they are locked: the players named
/// answered right, the others wrong.
/// </summary>
/// <param name="RoundId">The round the intent is aimed at.</param>
/// <param name="QuestionNumber">
/// The number of the question judged, from 1: the intent is obsolete once that question is judged or no longer in
/// progress, so that a judgment sent twice, or by two consoles, changes nothing: the first one wins.
/// </param>
/// <param name="AcceptedPlayers">The players whose answer is accepted, each of them having answered.</param>
public sealed record OpenQuestionJudge(RoundId RoundId, int QuestionNumber, ImmutableArray<PlayerId> AcceptedPlayers) : GameMasterRoundIntent(RoundId);
