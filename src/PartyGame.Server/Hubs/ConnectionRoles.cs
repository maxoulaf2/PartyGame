using Microsoft.AspNetCore.SignalR;
using PartyGame.Contracts;

namespace PartyGame.Server.Hubs;

/// <summary>
/// The role of a connection, or the player it identified as, kept in <see cref="HubCallerContext.Items"/> so that an
/// identity is never derived from a connection identifier.
/// </summary>
internal static class ConnectionRoles
{
    private static readonly object _roleKey = new();
    private static readonly object _playerKey = new();

    /// <summary>The role the connection announced, or <see langword="null"/> while it has none.</summary>
    public static Role? GetRole(this HubCallerContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.Items.TryGetValue(_roleKey, out var role) ? (Role?)role : null;
    }

    public static void SetRole(this HubCallerContext context, Role? role)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (role is null)
        {
            context.Items.Remove(_roleKey);
        }
        else
        {
            context.Items[_roleKey] = role.Value;
        }
    }

    /// <summary>The player the connection registered or identified as, or <see langword="null"/> while it has none.</summary>
    public static PlayerId? GetPlayerId(this HubCallerContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.Items.TryGetValue(_playerKey, out var playerId) ? (PlayerId?)playerId : null;
    }

    public static void SetPlayerId(this HubCallerContext context, PlayerId playerId)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Items[_playerKey] = playerId;
    }
}
