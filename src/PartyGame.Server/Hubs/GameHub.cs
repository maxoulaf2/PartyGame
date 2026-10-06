using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Http.Connections.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using PartyGame.Contracts;
using PartyGame.Engine;
using PartyGame.Engine.Projections;
using PartyGame.Server.FrontEnd;
using PartyGame.Server.GameMaster;
using PartyGame.Server.Games;
using PartyGame.Server.Incidents;
using PartyGame.Server.Network;
using PartyGame.Server.Packs;
using PartyGame.Server.Persistence;

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
    GamePersistence persistence,
    PlayerConnections playerConnections,
    PackReloader packReloader,
    IOptions<PersistenceOptions> persistenceOptions,
    IncidentJournal incidents,
    IIncidentReporter incidentReporter,
    MediaLocator mediaLocator,
    FrontEndBuild frontEndBuild,
    NetworkHealthJournal networkHealth,
    INetworkInterfaceSource networkInterfaces,
    TimeProvider timeProvider,
    ILogger<GameHub> logger) : Hub<IGameClient>
{
    /// <summary>SignalR target of <see cref="AnnounceAsync"/>, as the clients call it.</summary>
    public const string Announce = nameof(Announce);

    /// <summary>SignalR target of <see cref="JoinGameAsync"/>, as the clients call it.</summary>
    public const string JoinGame = nameof(JoinGame);

    /// <summary>SignalR target of <see cref="ResumeSessionAsync"/>, as the clients call it.</summary>
    public const string ResumeSession = nameof(ResumeSession);

    /// <summary>SignalR target of <see cref="LookUpReconnectionCode"/>, as the clients call it.</summary>
    public const string RecoverSession = nameof(RecoverSession);

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

    /// <summary>SignalR target of <see cref="SkipRoundAsync"/>, as the clients call it.</summary>
    public const string SkipRound = nameof(SkipRound);

    /// <summary>SignalR target of <see cref="ReturnToLobbyAsync"/>, as the clients call it.</summary>
    public const string ReturnToLobby = nameof(ReturnToLobby);

    /// <summary>SignalR target of <see cref="ShowJoinCodeAsync"/>, as the clients call it.</summary>
    public const string ShowJoinCode = nameof(ShowJoinCode);

    /// <summary>SignalR target of <see cref="SendRoundIntentAsync"/>, as the clients call it.</summary>
    public const string SendRoundIntent = nameof(SendRoundIntent);

    /// <summary>SignalR target of <see cref="SendGameMasterRoundIntentAsync"/>, as the clients call it.</summary>
    public const string SendGameMasterRoundIntent = nameof(SendGameMasterRoundIntent);

    /// <summary>SignalR target of <see cref="ResolveSavedGameAsync"/>, as the clients call it.</summary>
    public const string ResolveSavedGame = nameof(ResolveSavedGame);

    /// <summary>SignalR target of <see cref="CheckSavedGameMediaAsync"/>, as the clients call it.</summary>
    public const string CheckSavedGameMedia = nameof(CheckSavedGameMedia);

    /// <summary>SignalR target of <see cref="ReadClock"/>, as the clients call it.</summary>
    public const string SyncClock = nameof(SyncClock);

    /// <summary>SignalR target of <see cref="LogStaleBuild"/>, as the clients call it.</summary>
    public const string ReportStaleBuild = nameof(ReportStaleBuild);

    /// <summary>SignalR target of <see cref="LogClientErrorAsync"/>, as the clients call it.</summary>
    public const string ReportClientError = nameof(ReportClientError);

    /// <summary>SignalR target of <see cref="ReportDisplayMediaFailureAsync"/>, as the clients call it.</summary>
    public const string ReportDisplayMediaFailure = nameof(ReportDisplayMediaFailure);

    /// <summary>SignalR target of <see cref="DescribeConnection"/>, as the clients call it.</summary>
    public const string CheckNetwork = nameof(CheckNetwork);

    /// <summary>SignalR target of <see cref="RecordNetworkDiagnostic"/>, as the clients call it.</summary>
    public const string ReportNetworkDiagnostic = nameof(ReportNetworkDiagnostic);

    /// <summary>SignalR target of <see cref="RecordConnectionQuality"/>, as the clients call it.</summary>
    public const string ReportConnectionQuality = nameof(ReportConnectionQuality);

    /// <summary>SignalR target of <see cref="RecordDisplayAudio"/>, as the clients call it.</summary>
    public const string ReportDisplayAudio = nameof(ReportDisplayAudio);

    /// <summary>
    /// Longest round trip a page may report, in milliseconds: a longer one is no measure, and would only mislead the game
    /// master.
    /// </summary>
    private const int MaxRoundTrip = 60_000;

    /// <summary>Size of a player token: 128 random bits, out of reach of guessing.</summary>
    private const int TokenBytes = 16;

    /// <summary>
    /// The characters of a reconnection code: letters and digits, without those a player could confuse when reading them
    /// from the console (0 and O, 1 and I, L).
    /// </summary>
    private const string ReconnectionCodeCharacters = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";

    /// <summary>
    /// Length of a reconnection code: short to type on a phone, and 31^6, about 887 million codes, out of reach of a
    /// guess over the evening.
    /// </summary>
    private const int ReconnectionCodeLength = 6;

    /// <summary>
    /// Longest build identifier a page may report: those of <c>npm run build</c> are far shorter, and a longer one would
    /// only flood the logs.
    /// </summary>
    private const int MaxBuildIdLength = 64;

    /// <summary>
    /// Longest media identifier the TV screen may report: those the server draws are far shorter, and a longer one would
    /// only flood the logs.
    /// </summary>
    private const int MaxMediaIdLength = 64;

    private static readonly AnnouncementResult _accepted = new(Refusal: null);

    // The round trip a connection reported last, kept until it identifies as a player or the TV screen.
    private static readonly object _roundTripKey = new();

    /// <summary>
    /// Tells the new connection, before anything else, which client build the server serves: a page built otherwise is
    /// outdated and reloads itself. Sent again on every restored connection, which the server sees as a new one. It also
    /// tells whether the game master has yet to resolve the game found saved, which a phone waits for.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var pending = IsPending(game.State);
        await Clients.Caller.ReceiveWelcome(new Welcome(frontEndBuild.Id, pending)).ConfigureAwait(false);
        if (pending && !IsPending(game.State))
        {
            // Resolved meanwhile: its announcement to every connection may have come before this welcome.
            await Clients.Caller.ReceiveWelcome(new Welcome(frontEndBuild.Id, GamePending: false)).ConfigureAwait(false);
        }

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
            networkHealth.DisplayConnected(Transport(), ReportedRoundTrip());
            await Clients.Caller.ReceiveDisplaySnapshot(snapshots.ForDisplay(state)).ConfigureAwait(false);
        }
        else
        {
            await Clients.Caller.ReceiveGameMasterSnapshot(snapshots.ForGameMaster(state)).ConfigureAwait(false);

            // The same holds for the incidents: a newer list may come first, and the console keeps the newest.
            await Clients.Caller.ReceiveIncidents(incidents.Current).ConfigureAwait(false);
            await Clients.Caller.ReceiveNetworkHealth(networkHealth.Current).ConfigureAwait(false);
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
        var reconnectionCode = RandomNumberGenerator.GetString(ReconnectionCodeCharacters, ReconnectionCodeLength);
        var group = HubGroups.Player(playerId);

        // In the group before the loop broadcasts the new state, so that no snapshot sent meanwhile is missed.
        await Groups.AddToGroupAsync(Context.ConnectionId, group, Context.ConnectionAborted).ConfigureAwait(false);

        // Not cancelled with the connection: once enqueued, the registration may be accepted, and its connection must
        // then be tracked like any other, so that the player is shown disconnected.
        var outcome = await inputs
            .SubmitAsync(new Engine.Inputs.JoinGame(playerId, token, request.Nickname, timeProvider.GetUtcNow(), reconnectionCode), CancellationToken.None)
            .ConfigureAwait(false);
        if (outcome.Status != InputStatus.Accepted)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, group, CancellationToken.None).ConfigureAwait(false);
            return Refused(outcome.Rejection switch
            {
                RejectionReason.NicknameInvalid => JoinRefusal.NicknameInvalid,
                RejectionReason.NicknameTaken => JoinRefusal.NicknameTaken,
                RejectionReason.GamePending => JoinRefusal.GamePending,
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
        networkHealth.PlayerConnected(playerId, Transport(), ReportedRoundTrip());
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

        if (IsPending(game.State))
        {
            // The token may belong to the game found saved: kept until the game master decides.
            logger.ResumeWhileGamePending(Context.ConnectionId);
            return new ResumeSessionResult(ResumeSessionRefusal.GamePending, PlayerId: null);
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
        networkHealth.PlayerConnected(playerId, Transport(), ReportedRoundTrip());
        await Clients.Caller.ReceivePlayerSnapshot(snapshots.ForPlayer(state, player)).ConfigureAwait(false);
        return new ResumeSessionResult(Refusal: null, playerId);
    }

    /// <summary>
    /// Gives the token of the player whose reconnection code the message holds, to a phone that lost its token or never had
    /// it: the phone then resumes the session of the player with it, like after any reconnection. The connection itself
    /// stays anonymous until then.
    /// </summary>
    /// <remarks>
    /// The code is looked up in the state without going through the loop, like a token: codes are never removed, and the
    /// state read is immutable.
    /// </remarks>
    /// <param name="message">A <see cref="RecoverSessionRequest"/>.</param>
    [HubMethodName(RecoverSession)]
    public RecoverSessionResult LookUpReconnectionCode(JsonElement message)
    {
        if (!HubMessage.TryRead<RecoverSessionRequest>(message, out var request, out var invalidPath))
        {
            logger.MessageMalformed(RecoverSession, Context.ConnectionId, invalidPath);
            return new RecoverSessionResult(RecoverSessionRefusal.MessageInvalid, Token: null, LastClientSeq: 0);
        }

        var state = game.State;
        if (IsPending(state))
        {
            // The code may belong to the game found saved: the phone asks again once the game master decides.
            return new RecoverSessionResult(RecoverSessionRefusal.GamePending, Token: null, LastClientSeq: 0);
        }

        // ponytail: no limit on attempts, the size of the codes alone keeps a guess out of reach; count the failures of a
        // client address if one ever floods the logs.
        var code = request.Code.Trim().ToUpperInvariant();
        var player = state.Players.FirstOrDefault(p => state.ReconnectionCodes.GetValueOrDefault(p.Id) == code);
        if (player is null)
        {
            logger.ReconnectionCodeRejected(Context.ConnectionId);
            return new RecoverSessionResult(RecoverSessionRefusal.CodeUnknown, Token: null, LastClientSeq: 0);
        }

        // Every registered player has exactly one token.
        var token = state.PlayerTokens.First(entry => entry.Value == player.Id).Key;
        logger.SessionRecovered(player.Id.Value, Context.ConnectionId);
        return new RecoverSessionResult(Refusal: null, token.Value, player.LastClientSeq);
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
    /// Skips the round in progress at the request of the game master, without its game mode: the console offers it when the
    /// round keeps failing. The loop alone decides whether the request still names the round in progress, so that a double
    /// tap or two consoles skip a single round. Nothing is answered: the snapshots show the end of the round either way.
    /// </summary>
    /// <param name="message">A <see cref="SkipRoundRequest"/>.</param>
    [GameMasterOnly]
    [HubMethodName(SkipRound)]
    public async Task SkipRoundAsync(JsonElement message)
    {
        if (!HubMessage.TryRead<SkipRoundRequest>(message, out var request, out var invalidPath))
        {
            logger.MessageMalformed(SkipRound, Context.ConnectionId, invalidPath);
            return;
        }

        // Not cancelled with the connection: once enqueued, the request may be accepted whoever is left to see it.
        await inputs
            .SubmitAsync(new Engine.Inputs.SkipRound(request.RoundId, timeProvider.GetUtcNow()), CancellationToken.None)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Ends the game at the request of the game master, whatever its phase, and goes back to the lobby with the same
    /// players, for a new game without restarting the server. The loop alone decides whether the request still names the
    /// current game, so that a double tap or two consoles never end the new one. Nothing is answered: the snapshots show
    /// the lobby either way.
    /// </summary>
    /// <param name="message">A <see cref="ReturnToLobbyRequest"/>.</param>
    [GameMasterOnly]
    [HubMethodName(ReturnToLobby)]
    public async Task ReturnToLobbyAsync(JsonElement message)
    {
        if (!HubMessage.TryRead<ReturnToLobbyRequest>(message, out var request, out var invalidPath))
        {
            logger.MessageMalformed(ReturnToLobby, Context.ConnectionId, invalidPath);
            return;
        }

        // Not cancelled with the connection: once enqueued, the request may be accepted whoever is left to see it.
        var outcome = await inputs
            .SubmitAsync(new Engine.Inputs.ReturnToLobby(request.GameId, timeProvider.GetUtcNow()), CancellationToken.None)
            .ConfigureAwait(false);
        if (outcome.Status == InputStatus.Accepted)
        {
            logger.ReturnedToLobby(request.GameId.Value);
        }
    }

    /// <summary>
    /// Shows or hides the QR code on the TV screen at the request of the game master, outside the lobby which always shows
    /// it. The request names the outcome rather than toggling, so that a double tap or two consoles agree. Nothing is
    /// answered: the snapshots show the QR code either way.
    /// </summary>
    /// <param name="message">A <see cref="ShowJoinCodeRequest"/>.</param>
    [GameMasterOnly]
    [HubMethodName(ShowJoinCode)]
    public async Task ShowJoinCodeAsync(JsonElement message)
    {
        if (!HubMessage.TryRead<ShowJoinCodeRequest>(message, out var request, out var invalidPath))
        {
            logger.MessageMalformed(ShowJoinCode, Context.ConnectionId, invalidPath);
            return;
        }

        // Not cancelled with the connection: once enqueued, the request may be accepted whoever is left to see it.
        await inputs
            .SubmitAsync(new Engine.Inputs.ShowJoinCode(request.Shown, timeProvider.GetUtcNow()), CancellationToken.None)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Hands what a player does in the round in progress to its game mode, through the loop. Every game mode goes through
    /// this method: its intents are the types derived from <see cref="PlayerRoundIntent"/>. The call returns once the loop
    /// has handled the intent, accepted or not, and the state it led to is saved: the phone takes the answer for an
    /// acknowledgment, and sends the intent again, with the same number, when it never gets it. A server killed right
    /// after the acknowledgment thus never loses the intent. Whether the intent was accepted shows in the snapshots.
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
        await persistence.WaitUntilSavedAsync(CancellationToken.None).ConfigureAwait(false);
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
    /// Resumes the game the server found saved when it restarted, or starts a new game in the lobby with the packs read
    /// again, at the request of the game master. The loop alone decides whether the request still names the game found, so
    /// that a double tap or two consoles decide only once. Every connection is then welcomed again, for the phones to
    /// resume their session or register. Nothing is answered: the snapshots show the game either way.
    /// </summary>
    /// <param name="message">A <see cref="ResolveSavedGameRequest"/>.</param>
    [GameMasterOnly]
    [HubMethodName(ResolveSavedGame)]
    public async Task ResolveSavedGameAsync(JsonElement message)
    {
        if (!HubMessage.TryRead<ResolveSavedGameRequest>(message, out var request, out var invalidPath))
        {
            logger.MessageMalformed(ResolveSavedGame, Context.ConnectionId, invalidPath);
            return;
        }

        // Not cancelled with the connection: once enqueued, the decision may be accepted whoever is left to see it.
        var now = timeProvider.GetUtcNow();
        var outcome = await inputs
            .SubmitAsync(
                request.Resume ? new Engine.Inputs.ResumeSavedGame(request.SavedGameId, now) : new Engine.Inputs.DiscardSavedGame(request.SavedGameId, now),
                CancellationToken.None)
            .ConfigureAwait(false);
        if (outcome.Status != InputStatus.Accepted)
        {
            return;
        }

        logger.SavedGameResolved(request.SavedGameId.Value, request.Resume);
        if (!request.Resume)
        {
            // The packs may have been fixed while the game master decided.
            await packReloader.ReloadAsync(CancellationToken.None).ConfigureAwait(false);
        }

        await Clients.All.ReceiveWelcome(new Welcome(frontEndBuild.Id, GamePending: false)).ConfigureAwait(false);
    }

    /// <summary>
    /// Checks again the media files of the game found saved at the request of the game master, once they put them back on
    /// the disk. The files are read here, outside the loop; the result reaches every console through the snapshots.
    /// </summary>
    /// <remarks>No message: there is nothing to tell but the intent itself.</remarks>
    [GameMasterOnly]
    [HubMethodName(CheckSavedGameMedia)]
    public async Task CheckSavedGameMediaAsync()
    {
        if (game.State.PendingGame is not { } pending)
        {
            return;
        }

        // Not cancelled with the connection: the other consoles get the result whoever is left to see it.
        await inputs
            .SubmitAsync(new Engine.Inputs.SavedGameMediaChecked(pending.Game.GameId, PackMediaFiles.Missing(pending.Game, persistenceOptions.Value.FullPackCacheDirectory)), CancellationToken.None)
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
    /// <remarks>Kept apart from <see cref="LogClientErrorAsync"/>: an outdated build is no error of the page.</remarks>
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
    /// Logs for the operator a JavaScript error a page met without showing anything: the players, the TV screen and the
    /// game master never see it. Any connection may report one, identified or not, since an error may come before the
    /// announcement. Long fields are cut, and a connection that reports too much is ignored for a while. A view the TV
    /// screen could not render is also an incident for the game master, who may skip the round the public no longer sees;
    /// what fails on a phone or on the console stays in the logs, since the game master can do nothing about it.
    /// </summary>
    /// <param name="message">A <see cref="ClientErrorReport"/>.</param>
    [HubMethodName(ReportClientError)]
    public async Task LogClientErrorAsync(JsonElement message)
    {
        // Counted before reading, so that a flood of malformed reports is bounded as well.
        switch (ClientErrorAllowance.Of(Context).Admit(timeProvider.GetUtcNow()))
        {
            case ClientErrorAdmission.Dropped:
                return;
            case ClientErrorAdmission.FirstDropped:
                logger.ClientErrorsDropped(Context.ConnectionId, ClientErrorAllowance.ReportsPerWindow);
                return;
        }

        if (!HubMessage.TryRead<ClientErrorReport>(message, out var report, out var invalidPath))
        {
            logger.MessageMalformed(ReportClientError, Context.ConnectionId, invalidPath);
            return;
        }

        logger.ClientErrorReported(
            report.Kind,
            report.Role,
            ClientErrorFields.Truncate(report.Page, ClientErrorFields.MaxPageLength),
            Context.ConnectionId,
            Context.GetPlayerId()?.Value,
            ClientErrorFields.Truncate(report.Message, ClientErrorFields.MaxMessageLength),
            ClientErrorFields.Truncate(report.RoundViewType, ClientErrorFields.MaxRoundViewTypeLength),
            report.SnapshotVersion,
            ClientErrorFields.Truncate(report.BuildId, ClientErrorFields.MaxBuildIdLength),
            ClientErrorFields.Truncate(report.Stack, ClientErrorFields.MaxStackLength));

        // The role the connection announced, never the one the report claims: any page may claim to be the TV screen.
        if (report.Kind == ClientErrorKind.RenderFailed && Context.GetRole() == Role.Display)
        {
            // Not cancelled with the connection: the game master hears of it whoever is left on the TV screen.
            await incidentReporter
                .ReportIncidentAsync(IncidentCode.DisplayViewFailed, RoundInProgress(game.State), step: null, CancellationToken.None)
                .ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Tells the game master that the TV screen could not load a media file of the pack, with the round and the step of the
    /// round that show it: the TV screen knows the identifier of the file only. The TV screen alone may report it, and its
    /// reports count with its error reports, so that a connection that reports too much is ignored for a while.
    /// </summary>
    /// <param name="message">A <see cref="DisplayMediaFailureReport"/>.</param>
    [HubMethodName(ReportDisplayMediaFailure)]
    public async Task ReportDisplayMediaFailureAsync(JsonElement message)
    {
        switch (ClientErrorAllowance.Of(Context).Admit(timeProvider.GetUtcNow()))
        {
            case ClientErrorAdmission.Dropped:
                return;
            case ClientErrorAdmission.FirstDropped:
                logger.ClientErrorsDropped(Context.ConnectionId, ClientErrorAllowance.ReportsPerWindow);
                return;
        }

        if (!HubMessage.TryRead<DisplayMediaFailureReport>(message, out var report, out var invalidPath))
        {
            logger.MessageMalformed(ReportDisplayMediaFailure, Context.ConnectionId, invalidPath);
            return;
        }

        if (report.MediaId.Length > MaxMediaIdLength)
        {
            logger.MessageMalformed(ReportDisplayMediaFailure, Context.ConnectionId, "$.mediaId");
            return;
        }

        if (Context.GetRole() != Role.Display)
        {
            // Only the TV screen shows the media files: a failure reported by another page tells nothing of the room.
            logger.DisplayReportFromOtherRole(ReportDisplayMediaFailure, Context.ConnectionId);
            return;
        }

        var state = game.State;
        var id = new MediaId(report.MediaId);
        MediaLocation? location;
        try
        {
            location = mediaLocator.Locate(state, id);
        }
        catch (Exception ex)
        {
            // A bug of the game mode: the game master still hears of the file, without its step.
            logger.MediaNotLocated(ex, report.MediaId);
            location = state.Media.Find(id) is { } media ? new MediaLocation(media, RoundInProgress(state), Step: null) : null;
        }

        if (location is null)
        {
            // An identifier of another game, such as one a TV screen kept from before a restart of the server.
            logger.DisplayMediaUnknown(Context.ConnectionId, report.MediaId);
            return;
        }

        logger.DisplayMediaFailed(location.Media.Value, report.MediaId, location.Round?.Number, location.Step);

        // Not cancelled with the connection: the game master hears of it whoever is left on the TV screen.
        await incidentReporter
            .ReportIncidentAsync(IncidentCode.DisplayMediaFailed, location.Round, location.Step, CancellationToken.None)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Tells the diagnostic page what the server sees of its connection, for any connection, identified or not: how it
    /// reaches the hub, and whether the device belongs to the network of the address advertised to phones.
    /// </summary>
    [HubMethodName(CheckNetwork)]
    public NetworkCheckResult DescribeConnection() => new(
        Transport(),
        SubnetCheck.IsOnAdvertisedNetwork(
            networkInterfaces.GetInterfaces(),
            game.State.JoinAddress,
            Context.GetHttpContext()?.Connection.RemoteIpAddress));

    /// <summary>
    /// Records the outcome of a test of the diagnostic page, for the game master console to list it with the kind of the
    /// device, deduced from its browser. Any connection may report one, identified or not: the page does not register.
    /// </summary>
    /// <param name="message">A <see cref="NetworkDiagnosticReport"/>.</param>
    [HubMethodName(ReportNetworkDiagnostic)]
    public void RecordNetworkDiagnostic(JsonElement message)
    {
        if (!HubMessage.TryRead<NetworkDiagnosticReport>(message, out var report, out var invalidPath))
        {
            logger.MessageMalformed(ReportNetworkDiagnostic, Context.ConnectionId, invalidPath);
            return;
        }

        if (report.RoundTripMedian is < 0 or > MaxRoundTrip)
        {
            logger.MessageMalformed(ReportNetworkDiagnostic, Context.ConnectionId, "$.roundTripMedian");
            return;
        }

        var device = DeviceKinds.FromUserAgent(Context.GetHttpContext()?.Request.Headers.UserAgent);
        networkHealth.RecordDiagnostic(device, report.Verdict, report.RoundTripMedian);
        logger.NetworkDiagnosticReported(device, report.Verdict, report.RoundTripMedian);
    }

    /// <summary>
    /// Records the round trip a page measured with its last clock synchronization, for the game master to see how well each
    /// phone and the TV screen reach the server. Kept with the connection until it identifies: a phone synchronizes its
    /// clock as soon as it connects, before its player registers. Logs nothing: every page reports every minute.
    /// </summary>
    /// <param name="message">A <see cref="ConnectionQualityReport"/>.</param>
    [HubMethodName(ReportConnectionQuality)]
    public void RecordConnectionQuality(JsonElement message)
    {
        if (!HubMessage.TryRead<ConnectionQualityReport>(message, out var report, out var invalidPath))
        {
            logger.MessageMalformed(ReportConnectionQuality, Context.ConnectionId, invalidPath);
            return;
        }

        if (report.RoundTrip is < 0 or > MaxRoundTrip)
        {
            logger.MessageMalformed(ReportConnectionQuality, Context.ConnectionId, "$.roundTrip");
            return;
        }

        Context.Items[_roundTripKey] = report.RoundTrip;
        if (Context.GetPlayerId() is { } playerId)
        {
            networkHealth.RecordRoundTrip(playerId, report.RoundTrip);
        }
        else if (Context.GetRole() == Role.Display)
        {
            networkHealth.RecordRoundTrip(playerId: null, report.RoundTrip);
        }
    }

    /// <summary>
    /// Records whether the browser of the TV screen lets it play sound, for the game master console to warn while it does
    /// not. The TV screen alone may report it. Logs nothing: the console shows it.
    /// </summary>
    /// <param name="message">A <see cref="DisplayAudioReport"/>.</param>
    [HubMethodName(ReportDisplayAudio)]
    public void RecordDisplayAudio(JsonElement message)
    {
        if (!HubMessage.TryRead<DisplayAudioReport>(message, out var report, out var invalidPath))
        {
            logger.MessageMalformed(ReportDisplayAudio, Context.ConnectionId, invalidPath);
            return;
        }

        if (Context.GetRole() == Role.Display)
        {
            networkHealth.RecordDisplayAudio(report.Unlocked);
        }
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

    private static bool IsPending(GameState state) => state.Phase == GamePhase.ResumePending;

    /// <summary>
    /// The round in progress, or <see langword="null"/> outside a round: between two rounds, the last round played is over.
    /// </summary>
    private static RoundInfo? RoundInProgress(GameState state) =>
        state.Phase == GamePhase.Round ? Snapshots.RoundInfoOf(state) : null;

    private ConnectionTransport Transport() =>
        Context.Features.Get<IHttpTransportFeature>()?.TransportType switch
        {
            HttpTransportType.ServerSentEvents => ConnectionTransport.ServerSentEvents,
            HttpTransportType.LongPolling => ConnectionTransport.LongPolling,
            _ => ConnectionTransport.WebSockets,
        };

    private int? ReportedRoundTrip() => Context.Items.TryGetValue(_roundTripKey, out var roundTrip) ? (int?)roundTrip : null;

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
