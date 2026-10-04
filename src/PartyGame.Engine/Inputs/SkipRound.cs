using PartyGame.Contracts;

namespace PartyGame.Engine.Inputs;

/// <summary>
/// The game master skips the round in progress, which ends without its game mode. Only a connection authenticated as game
/// master gets it past the hub.
/// </summary>
/// <param name="RoundId">The round to skip: the request is obsolete once another one is in progress, or none is.</param>
/// <param name="ReceivedAt">Server time at which the hub received the intent.</param>
public sealed record SkipRound(RoundId RoundId, DateTimeOffset ReceivedAt) : Intent(ReceivedAt);
