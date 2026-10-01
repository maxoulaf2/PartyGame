namespace PartyGame.Contracts;

/// <summary>
/// The first message of the TV screen and of the game master console on a connection: it tells the hub which
/// projection the connection gets. Players identify themselves by registering or resuming their session instead.
/// </summary>
/// <param name="Role">The role claimed, <see cref="Role.Display"/> or <see cref="Role.GameMaster"/>.</param>
/// <param name="GameMasterCode">The code typed by the game master, required for <see cref="Role.GameMaster"/> and ignored otherwise.</param>
public sealed record Announcement(Role Role, string? GameMasterCode);
