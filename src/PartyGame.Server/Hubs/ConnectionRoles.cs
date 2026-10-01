using Microsoft.AspNetCore.SignalR;
using PartyGame.Contracts;

namespace PartyGame.Server.Hubs;

/// <summary>
/// The role of a connection, kept in <see cref="HubCallerContext.Items"/> once announced, so that an identity is never
/// derived from a connection identifier.
/// </summary>
internal static class ConnectionRoles
{
    private static readonly object _roleKey = new();

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
}
