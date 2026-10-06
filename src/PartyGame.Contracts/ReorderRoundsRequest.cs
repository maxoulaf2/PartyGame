using System.Collections.Immutable;

namespace PartyGame.Contracts;

/// <summary>
/// The game master changes the programme: the order of the rounds to come, and those withdrawn from it. The rounds played
/// and the one in progress never move.
/// </summary>
/// <param name="GameId">The game whose programme changes, which must be the current one.</param>
/// <param name="ExpectedOrder">
/// The rounds to come then those withdrawn, as the console shows them: a request sent twice, or by a second console that
/// does not see the last programme, is rejected as obsolete.
/// </param>
/// <param name="NewOrder">
/// The same rounds in their new order, each to come or withdrawn: the rounds to come are played in this order.
/// </param>
public sealed record ReorderRoundsRequest(GameId GameId, ImmutableArray<ScheduledRound> ExpectedOrder, ImmutableArray<ScheduledRound> NewOrder);
