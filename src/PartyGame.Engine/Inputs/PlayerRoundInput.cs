using PartyGame.Contracts;

namespace PartyGame.Engine.Inputs;

/// <summary>
/// A player acts in the round in progress. The hub only accepts it from a connection identified as a player.
/// </summary>
/// <param name="PlayerId">The player the connection identified as.</param>
/// <param name="RoundIntent">What the player wants to do, for the game mode of the round.</param>
/// <param name="ReceivedAt">Server time at which the hub received the intent.</param>
public sealed record PlayerRoundInput(PlayerId PlayerId, PlayerRoundIntent RoundIntent, DateTimeOffset ReceivedAt)
    : Intent(ReceivedAt);
