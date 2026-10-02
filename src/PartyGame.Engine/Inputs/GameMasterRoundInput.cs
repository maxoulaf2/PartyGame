using PartyGame.Contracts;

namespace PartyGame.Engine.Inputs;

/// <summary>
/// The game master acts in the round in progress. Only a connection authenticated as game master gets it past the hub.
/// </summary>
/// <param name="RoundIntent">What the game master wants to do, for the game mode of the round.</param>
/// <param name="ReceivedAt">Server time at which the hub received the intent.</param>
public sealed record GameMasterRoundInput(GameMasterRoundIntent RoundIntent, DateTimeOffset ReceivedAt) : Intent(ReceivedAt);
