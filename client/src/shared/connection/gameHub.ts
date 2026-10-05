import {
    HubConnectionBuilder,
    HubConnectionState,
    LogLevel,
    type HubConnection,
} from '@microsoft/signalr';
import type {
    Announcement,
    AnnouncementResult,
    ChooseAdvertisedAddressRequest,
    ChooseAdvertisedAddressResult,
    ClientErrorReport,
    DisplayMediaFailureReport,
    GameMasterRoundIntent,
    IGameClient,
    JoinRequest,
    JoinResult,
    NextRoundRequest,
    PlayerIntentEnvelope,
    ReloadPacksResult,
    ResolveSavedGameRequest,
    RenamePlayerRequest,
    RenamePlayerResult,
    ResumeSessionRequest,
    ResumeSessionResult,
    ClockSyncResult,
    ConnectionQualityReport,
    DisplayAudioReport,
    NetworkCheckResult,
    NetworkDiagnosticReport,
    SelectPackRequest,
    SelectPackResult,
    ShowJoinCodeRequest,
    SkipRoundRequest,
    StaleBuildReport,
    StartGameResult,
} from '../contracts';

/** Where the .NET server exposes its SignalR hub (`ServerPaths.GameHub`). */
export const gameHubUrl = '/hub/game';

/**
 * Hub methods the clients call, with their arguments and their answer. Mirrors `GameHub` on the
 * server, which reads each message with the conventions of the generated types.
 */
export interface GameHubMethods {
    Announce: { args: [announcement: Announcement]; result: AnnouncementResult };
    JoinGame: { args: [request: JoinRequest]; result: JoinResult };
    ResumeSession: { args: [request: ResumeSessionRequest]; result: ResumeSessionResult };
    // Null when the server ignores the intent: the connection is not authenticated as game master.
    RenamePlayer: { args: [request: RenamePlayerRequest]; result: RenamePlayerResult | null };
    StartGame: { args: []; result: StartGameResult | null };
    ChooseAdvertisedAddress: {
        args: [request: ChooseAdvertisedAddressRequest];
        result: ChooseAdvertisedAddressResult | null;
    };
    SelectPack: { args: [request: SelectPackRequest]; result: SelectPackResult | null };
    ReloadPacks: { args: []; result: ReloadPacksResult | null };
    // The next seven answer nothing: the snapshots show whether the intent was accepted.
    ResolveSavedGame: { args: [request: ResolveSavedGameRequest]; result: null };
    CheckSavedGameMedia: { args: []; result: null };
    NextRound: { args: [request: NextRoundRequest]; result: null };
    SkipRound: { args: [request: SkipRoundRequest]; result: null };
    ShowJoinCode: { args: [request: ShowJoinCodeRequest]; result: null };
    SendRoundIntent: { args: [envelope: PlayerIntentEnvelope]; result: null };
    SendGameMasterRoundIntent: { args: [intent: GameMasterRoundIntent]; result: null };
    SyncClock: { args: []; result: ClockSyncResult };
    ReportStaleBuild: { args: [report: StaleBuildReport]; result: null };
    ReportClientError: { args: [report: ClientErrorReport]; result: null };
    ReportDisplayMediaFailure: { args: [report: DisplayMediaFailureReport]; result: null };
    CheckNetwork: { args: []; result: NetworkCheckResult };
    ReportNetworkDiagnostic: { args: [report: NetworkDiagnosticReport]; result: null };
    ReportConnectionQuality: { args: [report: ConnectionQualityReport]; result: null };
    ReportDisplayAudio: { args: [report: DisplayAudioReport]; result: null };
}

/**
 * What became of an intent the server answers nothing to: handed to the server, or not sent
 * (connection lost, or the page not identified yet). Whether the server accepted it shows in the
 * next snapshot.
 */
export type IntentOutcome = 'sent' | 'unreachable';

/** The part of a SignalR connection this module relies on, so that tests can stand in for it. */
export type HubTransport = Pick<
    HubConnection,
    | 'start'
    | 'stop'
    | 'invoke'
    | 'on'
    | 'off'
    | 'onreconnecting'
    | 'onreconnected'
    | 'onclose'
    | 'state'
>;

/**
 * Delays before the attempts to restore a lost connection: quick at first, then every 10 s,
 * forever. A party never gives up on a phone, however long the outage.
 */
export const reconnectDelays: readonly number[] = [0, 1_000, 2_000, 5_000, 10_000];

/** The delay before the attempt that follows `previousAttempts` failed ones. */
export function reconnectDelay(previousAttempts: number): number {
    return reconnectDelays[Math.min(previousAttempts, reconnectDelays.length - 1)] ?? 10_000;
}

/**
 * Signals that the page may have its network back: it came back to the foreground, or the
 * browser went online. Returns a function that stops listening.
 */
export type WakeSource = (callback: () => void) => () => void;

/**
 * The connection of a page to the game hub, typed by the contracts. The only way components talk
 * to the server: none of them imports `@microsoft/signalr`.
 *
 * Once started, it never gives up: a lost connection is restored automatically, however long the
 * outage, and at once when the page comes back to the foreground (Safari on iOS suspends
 * WebSockets in the background).
 */
export interface GameConnection<Messages = IGameClient> {
    /** Connects, trying again until it succeeds. Rejects only if stopped meanwhile. */
    start(): Promise<void>;
    /** Disconnects for good: nothing is restored afterwards. */
    stop(): Promise<void>;
    /** Calls a hub method and resolves to its answer. */
    invoke<M extends keyof GameHubMethods>(
        method: M,
        ...args: GameHubMethods[M]['args']
    ): Promise<GameHubMethods[M]['result']>;
    /** Handles a message of the server. Returns a function that stops handling it. */
    on<M extends keyof Messages & string>(message: M, handler: Messages[M]): () => void;
    /**
     * Called whenever a connection is established: the first one, then each restored one, after
     * the `onReconnected` callbacks.
     */
    onConnected(callback: () => void): void;
    /** Called when the connection is lost; attempts to restore it go on until one succeeds. */
    onReconnecting(callback: () => void): void;
    /**
     * Called when a lost connection is restored. The server sees a brand new connection: the page
     * must announce or identify itself again to get back its role and its snapshots.
     */
    onReconnected(callback: () => void): void;
}

/** Creates the connection of the page to the game hub, not started yet. */
export function createGameConnection<Messages = IGameClient>(
    transport: HubTransport = buildHubConnection(),
    wake: WakeSource = pageWakeSource,
): GameConnection<Messages> {
    const lostCallbacks: (() => void)[] = [];
    const restoredCallbacks: (() => void)[] = [];
    const connectedCallbacks: (() => void)[] = [];
    const notify = (callbacks: (() => void)[]) => {
        for (const callback of callbacks) {
            callback();
        }
    };

    let stopped = true;
    let restarting = false;
    // Ends the wait before the next attempt of `connect`, to try at once.
    let skipWait: (() => void) | null = null;
    let stopWaking: (() => void) | null = null;

    const wait = (delay: number) =>
        new Promise<void>((resolve) => {
            const done = () => {
                clearTimeout(timer);
                skipWait = null;
                resolve();
            };
            const timer = setTimeout(done, delay);
            skipWait = done;
        });

    /** Starts the transport, trying again until it succeeds. Resolves to false if stopped first. */
    async function connect(): Promise<boolean> {
        for (let attempt = 0; !stopped; attempt++) {
            if (attempt > 0) {
                await wait(reconnectDelay(attempt - 1));
                if (stopped) {
                    break;
                }
            }
            try {
                await transport.start();
                return true;
            } catch {
                // Server or network unreachable: the next attempt follows.
            }
        }
        return false;
    }

    // The automatic reconnection of SignalR never gives up (see buildHubConnection), but the
    // server may still close the connection, and `tryNow` stops it on purpose: start it over.
    async function restart() {
        if (restarting) {
            return;
        }
        restarting = true;
        try {
            if (await connect()) {
                notifyRestored();
            }
        } finally {
            restarting = false;
        }
    }

    function tryNow() {
        if (stopped) {
            return;
        }
        if (skipWait) {
            skipWait();
        } else if (transport.state === HubConnectionState.Reconnecting) {
            // SignalR cannot shorten the delay it waits for: stopping it closes the connection,
            // which `restart` starts over at once.
            transport.stop().catch(() => {});
        }
    }

    function notifyRestored() {
        notify(restoredCallbacks);
        notify(connectedCallbacks);
    }

    transport.onreconnecting(() => notify(lostCallbacks));
    transport.onreconnected(notifyRestored);
    transport.onclose(() => {
        if (stopped) {
            return;
        }
        notify(lostCallbacks);
        void restart();
    });

    return {
        start: async () => {
            stopped = false;
            stopWaking ??= wake(tryNow);
            if (!(await connect())) {
                throw new Error('Connection stopped before it was established.');
            }
            notify(connectedCallbacks);
        },
        stop: () => {
            stopped = true;
            stopWaking?.();
            stopWaking = null;
            skipWait?.();
            return transport.stop();
        },
        invoke: (method, ...args) => transport.invoke(method, ...args),
        on: (message, handler) => {
            // SignalR types handlers loosely; the contracts give them their real signature.
            const callback = handler as (...args: unknown[]) => void;
            transport.on(message, callback);
            return () => transport.off(message, callback);
        },
        onReconnecting: (callback) => {
            lostCallbacks.push(callback);
        },
        onReconnected: (callback) => {
            restoredCallbacks.push(callback);
        },
        onConnected: (callback) => {
            connectedCallbacks.push(callback);
        },
    };
}

/** Wakes the connection when the page comes back to the foreground or the browser goes online. */
const pageWakeSource: WakeSource = (callback) => {
    const onVisibilityChange = () => {
        if (document.visibilityState === 'visible') {
            callback();
        }
    };
    document.addEventListener('visibilitychange', onVisibilityChange);
    window.addEventListener('online', callback);
    return () => {
        document.removeEventListener('visibilitychange', onVisibilityChange);
        window.removeEventListener('online', callback);
    };
};

function buildHubConnection(): HubConnection {
    return (
        new HubConnectionBuilder()
            .withUrl(gameHubUrl)
            // Never gives up, unlike the default policy and its four attempts.
            .withAutomaticReconnect({
                nextRetryDelayInMilliseconds: ({ previousRetryCount }) =>
                    reconnectDelay(previousRetryCount),
            })
            // Connection noise stays out of the console; failures are still reported.
            .configureLogging(LogLevel.Warning)
            .build()
    );
}
