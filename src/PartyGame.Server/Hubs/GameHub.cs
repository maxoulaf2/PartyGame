using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using PartyGame.Contracts;
using PartyGame.Engine.Projections;
using PartyGame.Server.GameMaster;
using PartyGame.Server.Games;

namespace PartyGame.Server.Hubs;

/// <summary>
/// The single SignalR hub, under <see cref="ServerPaths.GameHub"/>. Its methods never touch the game state: they check the
/// shape of a message, enrich it, and hand it over. A connection is anonymous until it announces its role.
/// </summary>
/// <remarks>
/// Messages arrive as raw JSON and are read by <see cref="HubMessage"/>: a message SignalR could not bind would be dropped
/// before reaching the hub, without the <c>Warning</c> the operator needs.
/// </remarks>
internal sealed class GameHub(GameMasterCode gameMasterCode, GameLoop game, ILogger<GameHub> logger) : Hub<IGameClient>
{
    /// <summary>SignalR target of <see cref="AnnounceAsync"/>, as the clients call it.</summary>
    public const string Announce = nameof(Announce);

    private static readonly AnnouncementResult _accepted = new(Refusal: null);

    /// <summary>
    /// Gives the connection the role it claims, puts it in the group of that role, and sends it the current snapshot of
    /// that role. Announcing again replaces the previous role, even when the new announcement is refused. A malformed
    /// message changes nothing.
    /// </summary>
    /// <param name="message">An <see cref="Announcement"/>.</param>
    [HubMethodName(Announce)]
    public async Task<AnnouncementResult> AnnounceAsync(JsonElement message)
    {
        if (!HubMessage.TryRead<Announcement>(message, out var announcement, out var invalidPath))
        {
            logger.MessageMalformed(Announce, Context.ConnectionId, invalidPath);
            return new AnnouncementResult(AnnouncementRefusal.MessageInvalid);
        }

        if (announcement.Role is not (Role.Display or Role.GameMaster))
        {
            // Players identify themselves by registering or by resuming their session, never by announcing.
            logger.MessageMalformed(Announce, Context.ConnectionId, "$.role");
            return new AnnouncementResult(AnnouncementRefusal.MessageInvalid);
        }

        await LeaveRoleAsync().ConfigureAwait(false);

        if (announcement.Role == Role.GameMaster && !gameMasterCode.Verify(announcement.GameMasterCode))
        {
            logger.GameMasterCodeRejected(Context.ConnectionId);
            return new AnnouncementResult(AnnouncementRefusal.GameMasterCodeInvalid);
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, HubGroups.Of(announcement.Role), Context.ConnectionAborted).ConfigureAwait(false);
        Context.SetRole(announcement.Role);
        logger.ConnectionAnnounced(Context.ConnectionId, announcement.Role);

        // Read once in the group: a change made since is broadcast to it as well, and the client keeps the newest version.
        var state = game.State;
        await (announcement.Role == Role.Display
            ? Clients.Caller.ReceiveDisplaySnapshot(Snapshots.ForDisplay(state))
            : Clients.Caller.ReceiveGameMasterSnapshot(Snapshots.ForGameMaster(state))).ConfigureAwait(false);
        return _accepted;
    }

    private async Task LeaveRoleAsync()
    {
        if (Context.GetRole() is { } previous)
        {
            Context.SetRole(null);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, HubGroups.Of(previous), Context.ConnectionAborted).ConfigureAwait(false);
        }
    }
}
