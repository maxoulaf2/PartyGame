using PartyGame.Contracts;

namespace PartyGame.Engine.Inputs;

/// <summary>
/// A player acts in the round in progress. The hub only accepts it from a connection identified as a player.
/// </summary>
/// <param name="PlayerId">The player the connection identified as.</param>
/// <param name="ClientSeq">
/// The number the phone gave the intent: the engine ignores it when not greater than the last one handled for the player,
/// so that an intent sent again after a lost connection is never handled twice.
/// </param>
/// <param name="RoundIntent">What the player wants to do, for the game mode of the round.</param>
/// <param name="ReceivedAt">Server time at which the hub received the intent.</param>
public sealed record PlayerRoundInput(PlayerId PlayerId, long ClientSeq, PlayerRoundIntent RoundIntent, DateTimeOffset ReceivedAt)
    : Intent(ReceivedAt);
