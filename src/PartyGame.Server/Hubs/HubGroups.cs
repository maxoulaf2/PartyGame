using PartyGame.Contracts;

namespace PartyGame.Server.Hubs;

/// <summary>
/// SignalR groups, one per projection: the broadcast sends each projection to its group only.
/// </summary>
internal static class HubGroups
{
    public const string Display = "display";

    public const string GameMaster = "gm";

    /// <summary>
    /// The group of the connections that announced <paramref name="role"/>. Players get a group each, see
    /// <see cref="Player"/>.
    /// </summary>
    public static string Of(Role role) => role switch
    {
        Role.Display => Display,
        Role.GameMaster => GameMaster,
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Only the TV screen and the game master announce a role."),
    };

    /// <summary>
    /// The group of every connection of one player, joined when the player registers or identifies: a player may have
    /// several connections, such as two tabs, and none of them defines who they are.
    /// </summary>
    public static string Player(PlayerId playerId) => $"player:{playerId.Value}";
}
