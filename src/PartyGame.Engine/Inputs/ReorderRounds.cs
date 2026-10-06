using System.Collections.Immutable;
using PartyGame.Contracts;

namespace PartyGame.Engine.Inputs;

/// <summary>
/// The game master changes the programme of the rounds. Only a connection authenticated as game master gets it past the hub.
/// </summary>
/// <param name="GameId">The game whose programme changes: the request is obsolete for another game.</param>
/// <param name="ExpectedOrder">The rounds to come then those withdrawn: the request is obsolete once the programme is another.</param>
/// <param name="NewOrder">The same rounds in their new order, each to come or withdrawn.</param>
/// <param name="ReceivedAt">Server time at which the hub received the intent.</param>
public sealed record ReorderRounds(
    GameId GameId,
    ImmutableArray<ScheduledRound> ExpectedOrder,
    ImmutableArray<ScheduledRound> NewOrder,
    DateTimeOffset ReceivedAt) : Intent(ReceivedAt);
