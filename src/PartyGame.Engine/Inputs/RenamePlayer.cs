using PartyGame.Contracts;

namespace PartyGame.Engine.Inputs;

/// <summary>
/// The game master renames a player. Only a connection authenticated as game master gets it past the hub.
/// </summary>
/// <param name="PlayerId">The player to rename.</param>
/// <param name="Nickname">New nickname as typed by the game master, normalized by the engine.</param>
/// <param name="ReceivedAt">Server time at which the hub received the intent.</param>
public sealed record RenamePlayer(PlayerId PlayerId, string Nickname, DateTimeOffset ReceivedAt) : Intent(ReceivedAt);
