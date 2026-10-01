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
    /// The group of the connections that announced <paramref name="role"/>. Players get a group each, joined when they
    /// identify themselves (US-E04-02).
    /// </summary>
    public static string Of(Role role) => role switch
    {
        Role.Display => Display,
        Role.GameMaster => GameMaster,
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Only the TV screen and the game master announce a role."),
    };
}
