using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using PartyGame.Contracts;
using PartyGame.Engine;
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
internal sealed class GameHub(
    GameMasterCode gameMasterCode,
    GameLoop game,
    IGameInputWriter inputs,
    PlayerConnections playerConnections,
    TimeProvider timeProvider,
    ILogger<GameHub> logger) : Hub<IGameClient>
{
    /// <summary>SignalR target of <see cref="AnnounceAsync"/>, as the clients call it.</summary>
    public const string Announce = nameof(Announce);

    /// <summary>SignalR target of <see cref="JoinGameAsync"/>, as the clients call it.</summary>
    public const string JoinGame = nameof(JoinGame);

    /// <summary>SignalR target of <see cref="ResumeSessionAsync"/>, as the clients call it.</summary>
    public const string ResumeSession = nameof(ResumeSession);

    /// <summary>SignalR target of <see cref="RenamePlayerAsync"/>, as the clients call it.</summary>
    public const string RenamePlayer = nameof(RenamePlayer);

    /// <summary>SignalR target of <see cref="StartGameAsync"/>, as the clients call it.</summary>
    public const string StartGame = nameof(StartGame);

    /// <summary>SignalR target of <see cref="ChooseAdvertisedAddressAsync"/>, as the clients call it.</summary>
    public const string ChooseAdvertisedAddress = nameof(ChooseAdvertisedAddress);

    /// <summary>Size of a player token: 128 random bits, out of reach of guessing.</summary>
    private const int TokenBytes = 16;

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

    /// <summary>
    /// Registers a new player under the nickname of the message, and makes this connection theirs: it gets their
    /// snapshots from then on. The identifier and the token are generated here, and the token leaves the server in the
    /// answer only. The loop alone decides whether the nickname is valid and free, one registration at a time.
    /// </summary>
    /// <param name="message">A <see cref="JoinRequest"/>.</param>
    [HubMethodName(JoinGame)]
    public async Task<JoinResult> JoinGameAsync(JsonElement message)
    {
        if (!HubMessage.TryRead<JoinRequest>(message, out var request, out var invalidPath))
        {
            logger.MessageMalformed(JoinGame, Context.ConnectionId, invalidPath);
            return Refused(JoinRefusal.MessageInvalid);
        }

        if (Context.GetPlayerId() is not null)
        {
            logger.JoinRepeated(Context.ConnectionId);
            return Refused(JoinRefusal.AlreadyJoined);
        }

        var playerId = new PlayerId(Guid.NewGuid());
        var token = new PlayerToken(Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(TokenBytes)));
        var group = HubGroups.Player(playerId);

        // In the group before the loop broadcasts the new state, so that no snapshot sent meanwhile is missed.
        await Groups.AddToGroupAsync(Context.ConnectionId, group, Context.ConnectionAborted).ConfigureAwait(false);

        // Not cancelled with the connection: once enqueued, the registration may be accepted, and its connection must
        // then be tracked like any other, so that the player is shown disconnected.
        var outcome = await inputs
            .SubmitAsync(new Engine.Inputs.JoinGame(playerId, token, request.Nickname, timeProvider.GetUtcNow()), CancellationToken.None)
            .ConfigureAwait(false);
        if (outcome.Status != InputStatus.Accepted)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, group, CancellationToken.None).ConfigureAwait(false);
            return Refused(outcome.Rejection switch
            {
                RejectionReason.NicknameInvalid => JoinRefusal.NicknameInvalid,
                RejectionReason.NicknameTaken => JoinRefusal.NicknameTaken,
                _ => JoinRefusal.JoinFailed,
            });
        }

        Context.SetPlayerId(playerId);
        playerConnections.Register(playerId, Context.ConnectionId);
        if (Context.ConnectionAborted.IsCancellationRequested)
        {
            // The connection closed while the loop handled the registration: OnDisconnectedAsync may have run before the
            // connection was tracked, and the player must not stay shown as connected.
            await ReportConnectionLostAsync(playerId).ConfigureAwait(false);
            return Refused(JoinRefusal.JoinFailed);
        }

        // Read once registered and in the group: a change made since is broadcast as well, and the phone keeps the newest.
        var state = game.State;
        var player = state.Players.First(p => p.Id == playerId);
        logger.PlayerJoined(playerId.Value, player.Nickname);
        await Clients.Caller.ReceivePlayerSnapshot(Snapshots.ForPlayer(state, player)).ConfigureAwait(false);
        return new JoinResult(Refusal: null, playerId, token.Value);
    }

    /// <summary>
    /// Makes this connection the one of the player the token identifies, after a reload, a sleep or a lost network: it
    /// gets their snapshots from then on, starting with the current one, and the player is shown connected again. The
    /// identity comes from the token alone, never from a connection identifier.
    /// </summary>
    /// <remarks>
    /// The token is looked up in the state without going through the loop: tokens are never removed, and the state read
    /// is immutable. Only the change of presence goes through the loop.
    /// </remarks>
    /// <param name="message">A <see cref="ResumeSessionRequest"/>.</param>
    [HubMethodName(ResumeSession)]
    public async Task<ResumeSessionResult> ResumeSessionAsync(JsonElement message)
    {
        if (!HubMessage.TryRead<ResumeSessionRequest>(message, out var request, out var invalidPath))
        {
            logger.MessageMalformed(ResumeSession, Context.ConnectionId, invalidPath);
            return new ResumeSessionResult(ResumeSessionRefusal.MessageInvalid, PlayerId: null);
        }

        if (Context.GetPlayerId() is not null)
        {
            logger.ResumeRepeated(Context.ConnectionId);
            return new ResumeSessionResult(ResumeSessionRefusal.AlreadyIdentified, PlayerId: null);
        }

        if (!game.State.PlayerTokens.TryGetValue(new PlayerToken(request.Token), out var playerId))
        {
            logger.SessionUnknown(Context.ConnectionId);
            return new ResumeSessionResult(ResumeSessionRefusal.SessionUnknown, PlayerId: null);
        }

        // In the group before the loop shows the player connected, so that no snapshot sent meanwhile is missed.
        await Groups.AddToGroupAsync(Context.ConnectionId, HubGroups.Player(playerId), Context.ConnectionAborted).ConfigureAwait(false);
        Context.SetPlayerId(playerId);
        await playerConnections.ResumeAsync(playerId, Context.ConnectionId).ConfigureAwait(false);
        if (Context.ConnectionAborted.IsCancellationRequested)
        {
            // The connection closed meanwhile: OnDisconnectedAsync may have run before the connection was tracked, and
            // the player must not stay shown as connected.
            await ReportConnectionLostAsync(playerId).ConfigureAwait(false);
            return new ResumeSessionResult(ResumeSessionRefusal.ResumeFailed, PlayerId: null);
        }

        // Read once in the group: a change made since is broadcast as well, and the phone keeps the newest. Players are
        // never removed, so the player of a known token is there.
        var state = game.State;
        var player = state.Players.First(p => p.Id == playerId);
        logger.SessionResumed(playerId.Value, Context.ConnectionId);
        await Clients.Caller.ReceivePlayerSnapshot(Snapshots.ForPlayer(state, player)).ConfigureAwait(false);
        return new ResumeSessionResult(Refusal: null, playerId);
    }

    /// <summary>
    /// Renames a player at the request of the game master. The loop alone decides whether the nickname is valid and
    /// free; the new nickname reaches every client through the snapshots.
    /// </summary>
    /// <param name="message">A <see cref="RenamePlayerRequest"/>.</param>
    [GameMasterOnly]
    [HubMethodName(RenamePlayer)]
    public async Task<RenamePlayerResult> RenamePlayerAsync(JsonElement message)
    {
        if (!HubMessage.TryRead<RenamePlayerRequest>(message, out var request, out var invalidPath))
        {
            logger.MessageMalformed(RenamePlayer, Context.ConnectionId, invalidPath);
            return new RenamePlayerResult(RenamePlayerRefusal.MessageInvalid);
        }

        // Not cancelled with the connection: once enqueued, the rename may be accepted whoever is left to hear the answer.
        var outcome = await inputs
            .SubmitAsync(new Engine.Inputs.RenamePlayer(request.PlayerId, request.Nickname, timeProvider.GetUtcNow()), CancellationToken.None)
            .ConfigureAwait(false);
        if (outcome.Status != InputStatus.Accepted)
        {
            return new RenamePlayerResult(outcome.Rejection switch
            {
                RejectionReason.NicknameInvalid => RenamePlayerRefusal.NicknameInvalid,
                RejectionReason.NicknameTaken => RenamePlayerRefusal.NicknameTaken,
                RejectionReason.PlayerUnknown => RenamePlayerRefusal.PlayerUnknown,
                _ => RenamePlayerRefusal.RenameFailed,
            });
        }

        // Logged as normalized by the engine. Players are never removed, so the renamed one is still there.
        var renamed = game.State.Players.First(p => p.Id == request.PlayerId);
        logger.PlayerRenamed(request.PlayerId.Value, renamed.Nickname);
        return new RenamePlayerResult(Refusal: null);
    }

    /// <summary>
    /// Starts the game at the request of the game master. The loop alone decides whether it can start, so that two game
    /// masters tapping at once start it only once; the new phase reaches every client through the snapshots.
    /// </summary>
    /// <remarks>No message: there is nothing to tell but the intent itself.</remarks>
    [GameMasterOnly]
    [HubMethodName(StartGame)]
    public async Task<StartGameResult> StartGameAsync()
    {
        // Not cancelled with the connection: once enqueued, the start may be accepted whoever is left to hear the answer.
        var outcome = await inputs
            .SubmitAsync(new Engine.Inputs.StartGame(timeProvider.GetUtcNow()), CancellationToken.None)
            .ConfigureAwait(false);
        if (outcome.Status != InputStatus.Accepted)
        {
            return new StartGameResult(outcome.Rejection switch
            {
                RejectionReason.NotEnoughPlayers => StartGameRefusal.NotEnoughPlayers,
                RejectionReason.GameAlreadyStarted => StartGameRefusal.AlreadyStarted,
                _ => StartGameRefusal.StartFailed,
            });
        }

        logger.GameStarted(game.State.Players.Length);
        return new StartGameResult(Refusal: null);
    }

    /// <summary>
    /// Changes the address encoded in the QR code at the request of the game master, among the candidates detected at
    /// startup. The choice is not written to the configuration: a restart forgets it. The new address reaches the TV
    /// screen through the snapshots.
    /// </summary>
    /// <param name="message">A <see cref="ChooseAdvertisedAddressRequest"/>.</param>
    [GameMasterOnly]
    [HubMethodName(ChooseAdvertisedAddress)]
    public async Task<ChooseAdvertisedAddressResult> ChooseAdvertisedAddressAsync(JsonElement message)
    {
        if (!HubMessage.TryRead<ChooseAdvertisedAddressRequest>(message, out var request, out var invalidPath))
        {
            logger.MessageMalformed(ChooseAdvertisedAddress, Context.ConnectionId, invalidPath);
            return new ChooseAdvertisedAddressResult(ChooseAdvertisedAddressRefusal.MessageInvalid);
        }

        // Not cancelled with the connection: once enqueued, the choice may be accepted whoever is left to hear the answer.
        var outcome = await inputs
            .SubmitAsync(new Engine.Inputs.ChooseAdvertisedAddress(request.Address, timeProvider.GetUtcNow()), CancellationToken.None)
            .ConfigureAwait(false);
        if (outcome.Status != InputStatus.Accepted)
        {
            return new ChooseAdvertisedAddressResult(outcome.Rejection == RejectionReason.AddressUnknown
                ? ChooseAdvertisedAddressRefusal.AddressUnknown
                : ChooseAdvertisedAddressRefusal.ChoiceFailed);
        }

        logger.AdvertisedAddressChosen(request.Address);
        return new ChooseAdvertisedAddressResult(Refusal: null);
    }

    /// <summary>
    /// Reports to the loop that a player lost their last connection, so that the TV screen and the game master show them
    /// disconnected. The player stays registered.
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (Context.GetPlayerId() is { } playerId)
        {
            await ReportConnectionLostAsync(playerId).ConfigureAwait(false);
        }

        await base.OnDisconnectedAsync(exception).ConfigureAwait(false);
    }

    private static JoinResult Refused(JoinRefusal refusal) => new(refusal, PlayerId: null, Token: null);

    private async Task ReportConnectionLostAsync(PlayerId playerId)
    {
        bool wasLast;
        try
        {
            // False when another connection of the player is still open, or this one was already reported.
            wasLast = await playerConnections.DisconnectAsync(playerId, Context.ConnectionId).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return; // the server is stopping: nobody is left to show the player as disconnected
        }

        if (wasLast)
        {
            logger.PlayerDisconnected(playerId.Value);
        }
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
