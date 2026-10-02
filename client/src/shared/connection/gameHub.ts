import { HubConnectionBuilder, LogLevel, type HubConnection } from '@microsoft/signalr';
import type {
    Announcement,
    AnnouncementResult,
    ChooseAdvertisedAddressRequest,
    ChooseAdvertisedAddressResult,
    IGameClient,
    JoinRequest,
    JoinResult,
    RenamePlayerRequest,
    RenamePlayerResult,
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
    // Null when the server ignores the intent: the connection is not authenticated as game master.
    RenamePlayer: { args: [request: RenamePlayerRequest]; result: RenamePlayerResult | null };
    StartGame: { args: []; result: StartGameResult | null };
    ChooseAdvertisedAddress: {
        args: [request: ChooseAdvertisedAddressRequest];
        result: ChooseAdvertisedAddressResult | null;
    };
}

/** The part of a SignalR connection this module relies on, so that tests can stand in for it. */
export type HubTransport = Pick<
    HubConnection,
    'start' | 'stop' | 'invoke' | 'on' | 'off' | 'onreconnecting' | 'onreconnected' | 'onclose'
>;

/**
 * The connection of a page to the game hub, typed by the contracts. The only way components talk
 * to the server: none of them imports `@microsoft/signalr`.
 */
export interface GameConnection<Messages = IGameClient> {
    start(): Promise<void>;
    stop(): Promise<void>;
    /** Calls a hub method and resolves to its answer. */
    invoke<M extends keyof GameHubMethods>(
        method: M,
        ...args: GameHubMethods[M]['args']
    ): Promise<GameHubMethods[M]['result']>;
    /** Handles a message of the server. Returns a function that stops handling it. */
    on<M extends keyof Messages & string>(message: M, handler: Messages[M]): () => void;
    /** Called when the connection is lost and an attempt to restore it begins. */
    onReconnecting(callback: () => void): void;
    /**
     * Called when a lost connection is restored. The server sees a brand new connection: the page
     * must announce itself again to get back its role and its snapshots.
     */
    onReconnected(callback: () => void): void;
    /** Called when the connection is lost for good, or stopped. */
    onClose(callback: () => void): void;
}

/** Creates the connection of the page to the game hub, not started yet. */
export function createGameConnection<Messages = IGameClient>(
    transport: HubTransport = buildHubConnection(),
): GameConnection<Messages> {
    return {
        start: () => transport.start(),
        stop: () => transport.stop(),
        invoke: (method, ...args) => transport.invoke(method, ...args),
        on: (message, handler) => {
            // SignalR types handlers loosely; the contracts give them their real signature.
            const callback = handler as (...args: unknown[]) => void;
            transport.on(message, callback);
            return () => transport.off(message, callback);
        },
        onReconnecting: (callback) => transport.onreconnecting(() => callback()),
        onReconnected: (callback) => transport.onreconnected(() => callback()),
        onClose: (callback) => transport.onclose(() => callback()),
    };
}

function buildHubConnection(): HubConnection {
    return (
        new HubConnectionBuilder()
            .withUrl(gameHubUrl)
            // Default policy for now; retrying forever and restarting after a close is US-E05-01.
            .withAutomaticReconnect()
            // Connection noise stays out of the console; failures are still reported.
            .configureLogging(LogLevel.Warning)
            .build()
    );
}
