namespace PartyGame.Contracts;

/// <summary>
/// The game master renames a player, to replace a nickname that is out of place or unreadable.
/// </summary>
/// <param name="PlayerId">The player to rename.</param>
/// <param name="Nickname">
/// The new nickname as typed. The server normalizes it and applies the rules of the registration.
/// </param>
public sealed record RenamePlayerRequest(PlayerId PlayerId, string Nickname);
