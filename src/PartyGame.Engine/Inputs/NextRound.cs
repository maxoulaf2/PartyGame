using PartyGame.Contracts;

namespace PartyGame.Engine.Inputs;

/// <summary>
/// The game master asks for the next round. Only a connection authenticated as game master gets it past the hub.
/// </summary>
/// <param name="AfterRound">The round that just finished: the request is obsolete once another one started.</param>
/// <param name="ReceivedAt">Server time at which the hub received the intent.</param>
public sealed record NextRound(RoundId AfterRound, DateTimeOffset ReceivedAt) : Intent(ReceivedAt);
