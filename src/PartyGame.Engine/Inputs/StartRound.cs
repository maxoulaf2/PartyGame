using PartyGame.Contracts;

namespace PartyGame.Engine.Inputs;

/// <summary>
/// The game master starts the round announced. Only a connection authenticated as game master gets it past the hub.
/// </summary>
/// <param name="RoundId">The round announced: the request is obsolete once it started.</param>
/// <param name="ReceivedAt">Server time at which the hub received the intent.</param>
public sealed record StartRound(RoundId RoundId, DateTimeOffset ReceivedAt) : Intent(ReceivedAt);
