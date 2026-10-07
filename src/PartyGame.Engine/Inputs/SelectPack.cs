using PartyGame.Engine.State;

namespace PartyGame.Engine.Inputs;

/// <summary>
/// The game master chooses the pack of the game, in the lobby. Only a connection authenticated as game master gets it past
/// the hub.
/// </summary>
/// <param name="PackId">The identifier of a valid pack of <see cref="GameState.Catalog"/>.</param>
/// <param name="ReceivedAt">Server time at which the hub received the intent.</param>
public sealed record SelectPack(string PackId, DateTimeOffset ReceivedAt) : Intent(ReceivedAt);
