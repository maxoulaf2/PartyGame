using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using PartyGame.Contracts;
using PartyGame.Engine;
using PartyGame.Engine.Projections;
using PartyGame.Server.FrontEnd;
using PartyGame.Server.GameMaster;
using PartyGame.Server.Games;
using PartyGame.Server.Incidents;
using PartyGame.Server.Packs;

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
    Snapshots snapshots,
    IGameInputWriter inputs,
    PlayerConnections playerConnections,
    PackReloader packReloader,
    IncidentJournal incidents,
    FrontEndBuild frontEndBuild,
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

    /// <summary>SignalR target of <see cref="SelectPackAsync"/>, as the clients call it.</summary>
    public const string SelectPack = nameof(SelectPack);

    /// <summary>SignalR target of <see cref="ReloadPacksAsync"/>, as the clients call it.</summary>
    public const string ReloadPacks = nameof(ReloadPacks);

    /// <summary>SignalR target of <see cref="NextRoundAsync"/>, as the clients call it.</summary>
    public const string NextRound = nameof(NextRound);

    /// <summary>SignalR target of <see cref="SendRoundIntentAsync"/>, as the clients call it.</summary>
    public const string SendRoundIntent = nameof(SendRoundIntent);

    /// <summary>SignalR target of <see cref="SendGameMasterRoundIntentAsync"/>, as the clients call it.</summary>
    public const string SendGameMasterRoundIntent = nameof(SendGameMasterRoundIntent);

    /// <summary>SignalR target of <see cref="ReadClock"/>, as the clients call it.</summary>
    public const string SyncClock = nameof(SyncClock);

    /// <summary>SignalR target of <see cref="LogStaleBuild"/>, as the clients call it.</summary>
    public const string ReportStaleBuild = nameof(ReportStaleBuild);

    /// <summary>Size of a player token: 128 random bits, out of reach of guessing.</summary>
    private const int TokenBytes = 16;

    /// <summary>
    /// Longest build identifier a page may report: those of <c>npm run build</c> are far shorter, and a longer one would
    /// only flood the logs.
    /// </summary>
    private const int MaxBuildIdLength = 64;

    private static readonly AnnouncementResult _accepted = new(Refusal: null);

    /// <summary>
    /// Tells the new connection, before anything else, which client build the server serves: a page built otherwise is
    /// outdated and reloads itself. Sent again on every restored connection, which the server sees as a new one.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        await Clients.Caller.ReceiveWelcome(new Welcome(frontEndBuild.Id)).ConfigureAwait(false);
        await base.OnConnectedAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Gives the connection the role it claims, puts it in the group of that role, and sends it the current snapshot of
    /// that role, and to the game master the incidents of the server as well. Announcing again replaces the previous role, even when the new announcement is refused. A malformed
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
        if (announcement.Role == Role.Display)
        {
            await Clients.Caller.ReceiveDisplaySnapshot(snapshots.ForDisplay(state)).ConfigureAwait(false);
        }
        else
        {
            await Clients.Caller.ReceiveGameMasterSnapshot(snapshots.ForGameMaster(state)).ConfigureAwait(false);

            // The same holds for the incidents: a newer list may come first, and the console keeps the newest.
            await Clients.Caller.ReceiveIncidents(incidents.Current).ConfigureAwait(false);
        }

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
        await Clients.Caller.ReceivePlayerSnapshot(snapshots.ForPlayer(state, player)).ConfigureAwait(false);
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
        await Clients.Caller.ReceivePlayerSnapshot(snapshots.ForPlayer(state, player)).ConfigureAwait(false);
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
                RejectionReason.PackNotSelected => StartGameRefusal.PackNotSelected,
                RejectionReason.GameAlreadyStarted => StartGameRefusal.AlreadyStarted,
                _ => StartGameRefusal.StartFailed,
            });
        }

        var started = game.State;
        logger.GameStarted(started.Players.Length, started.SelectedPackId!, started.Pack!.Title);
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
    /// Chooses the pack of the game at the request of the game master, in the lobby. The loop alone decides whether the pack
    /// can be chosen; the selection reaches every console and the TV screen through the snapshots. Choosing the selected
    /// pack again changes nothing.
    /// </summary>
    /// <param name="message">A <see cref="SelectPackRequest"/>.</param>
    [GameMasterOnly]
    [HubMethodName(SelectPack)]
    public async Task<SelectPackResult> SelectPackAsync(JsonElement message)
    {
        if (!HubMessage.TryRead<SelectPackRequest>(message, out var request, out var invalidPath))
        {
            logger.MessageMalformed(SelectPack, Context.ConnectionId, invalidPath);
            return new SelectPackResult(SelectPackRefusal.MessageInvalid);
        }

        // Not cancelled with the connection: once enqueued, the choice may be accepted whoever is left to hear the answer.
        var outcome = await inputs
            .SubmitAsync(new Engine.Inputs.SelectPack(request.PackId, timeProvider.GetUtcNow()), CancellationToken.None)
            .ConfigureAwait(false);
        if (outcome.Status != InputStatus.Accepted)
        {
            return new SelectPackResult(outcome.Rejection switch
            {
                RejectionReason.PackUnknown => SelectPackRefusal.PackUnknown,
                RejectionReason.PackInvalid => SelectPackRefusal.PackInvalid,
                RejectionReason.GameAlreadyStarted => SelectPackRefusal.AlreadyStarted,
                _ => SelectPackRefusal.SelectionFailed,
            });
        }

        logger.PackSelected(request.PackId);
        return new SelectPackResult(Refusal: null);
    }

    /// <summary>
    /// Loads and checks the packs again at the request of the game master, once they fixed a pack on the disk. The loop
    /// refuses the result once the game is started; the new catalog reaches every console through the snapshots, and the
    /// selection is cancelled if its pack is no longer valid.
    /// </summary>
    /// <remarks>No message: there is nothing to tell but the intent itself.</remarks>
    [GameMasterOnly]
    [HubMethodName(ReloadPacks)]
    public async Task<ReloadPacksResult> ReloadPacksAsync()
    {
        // Not cancelled with the connection: the other consoles get the new catalog whoever is left to hear the answer.
        var outcome = await packReloader.ReloadAsync(CancellationToken.None).ConfigureAwait(false);
        if (outcome.Status != InputStatus.Accepted)
        {
            return new ReloadPacksResult(outcome.Rejection == RejectionReason.GameAlreadyStarted
                ? ReloadPacksRefusal.AlreadyStarted
                : ReloadPacksRefusal.ReloadFailed);
        }

        var catalog = game.State.Catalog;
        var validCount = catalog.Packs.Count(pack => pack.IsValid);
        logger.PacksReloaded(catalog.Packs.Length, validCount);
        return new ReloadPacksResult(Refusal: null);
    }

    /// <summary>
    /// Starts the next round at the request of the game master, between two rounds. The loop alone decides whether the
    /// request still names the round that just finished, so that a double tap or two consoles start the next round only
    /// once. Nothing is answered: the snapshots show the round in progress either way.
    /// </summary>
    /// <param name="message">A <see cref="NextRoundRequest"/>.</param>
    [GameMasterOnly]
    [HubMethodName(NextRound)]
    public async Task NextRoundAsync(JsonElement message)
    {
        if (!HubMessage.TryRead<NextRoundRequest>(message, out var request, out var invalidPath))
        {
            logger.MessageMalformed(NextRound, Context.ConnectionId, invalidPath);
            return;
        }

        // Not cancelled with the connection: once enqueued, the request may be accepted whoever is left to see it.
        await inputs
            .SubmitAsync(new Engine.Inputs.NextRound(request.AfterRound, timeProvider.GetUtcNow()), CancellationToken.None)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Hands what a player does in the round in progress to its game mode, through the loop. Every game mode goes through
    /// this method: its intents are the types derived from <see cref="PlayerRoundIntent"/>. The call returns once the loop
    /// has handled the intent, accepted or not: the phone takes the answer for an acknowledgment, and sends the intent
    /// again, with the same number, when it never gets it. Whether the intent was accepted shows in the snapshots.
    /// </summary>
    /// <param name="message">A <see cref="PlayerIntentEnvelope"/>, its intent of the type its <c>type</c> names.</param>
    [HubMethodName(SendRoundIntent)]
    public async Task SendRoundIntentAsync(JsonElement message)
    {
        if (!HubMessage.TryRead<PlayerIntentEnvelope>(message, out var envelope, out var invalidPath))
        {
            logger.MessageMalformed(SendRoundIntent, Context.ConnectionId, invalidPath);
            return;
        }

        if (envelope.ClientSeq < 1)
        {
            // Numbers start from 1: the engine would take any other for an intent already handled.
            logger.MessageMalformed(SendRoundIntent, Context.ConnectionId, "$.clientSeq");
            return;
        }

        if (Context.GetPlayerId() is not { } playerId)
        {
            // Only the connection of a player may act for them: the identity comes from the session, never the message.
            logger.RoundIntentWithoutPlayer(Context.ConnectionId);
            return;
        }

        var input = new Engine.Inputs.PlayerRoundInput(playerId, envelope.ClientSeq, envelope.Intent, timeProvider.GetUtcNow());

        // Not cancelled with the connection: once enqueued, the intent may be accepted whoever is left to see it.
        await inputs.SubmitAsync(input, CancellationToken.None).ConfigureAwait(false);
    }

    /// <summary>
    /// Hands what the game master does in the round in progress to its game mode, through the loop. Every game mode goes
    /// through this method: its intents are the types derived from <see cref="GameMasterRoundIntent"/>. The call returns
    /// once the loop has handled the intent; whether it was accepted shows in the snapshots.
    /// </summary>
    /// <param name="message">A <see cref="GameMasterRoundIntent"/>, of the type its <c>type</c> names.</param>
    [GameMasterOnly]
    [HubMethodName(SendGameMasterRoundIntent)]
    public async Task SendGameMasterRoundIntentAsync(JsonElement message)
    {
        if (!HubMessage.TryRead<GameMasterRoundIntent>(message, out var intent, out var invalidPath))
        {
            logger.MessageMalformed(SendGameMasterRoundIntent, Context.ConnectionId, invalidPath);
            return;
        }

        // Not cancelled with the connection: once enqueued, the intent may be accepted whoever is left to see it.
        await inputs
            .SubmitAsync(new Engine.Inputs.GameMasterRoundInput(intent, timeProvider.GetUtcNow()), CancellationToken.None)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Answers the current time of the server, for any connection, identified or not: the clients estimate from it the
    /// offset of their clock, as NTP does. A mere reading of the clock, it never goes through the loop, whose queue would
    /// add a variable delay to the measure, and logs nothing: every client calls it in bursts.
    /// </summary>
    [HubMethodName(SyncClock)]
    public ClockSyncResult ReadClock() => new(timeProvider.GetUtcNow().ToUnixTimeMilliseconds());

    /// <summary>
    /// Logs for the operator that a page still runs another build than the one served, although it reloaded to get it: a
    /// stubborn cache or a proxy. The page goes on with its build. Any connection may report it, identified or not.
    /// </summary>
    /// <remarks>A minimal report, until the clients report their errors in general (E10).</remarks>
    /// <param name="message">A <see cref="StaleBuildReport"/>.</param>
    [HubMethodName(ReportStaleBuild)]
    public void LogStaleBuild(JsonElement message)
    {
        if (!HubMessage.TryRead<StaleBuildReport>(message, out var report, out var invalidPath))
        {
            logger.MessageMalformed(ReportStaleBuild, Context.ConnectionId, invalidPath);
            return;
        }

        if (report.ClientBuildId.Length > MaxBuildIdLength)
        {
            logger.MessageMalformed(ReportStaleBuild, Context.ConnectionId, "$.clientBuildId");
            return;
        }

        logger.StaleBuildReported(Context.ConnectionId, report.ClientBuildId, frontEndBuild.Id);
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
