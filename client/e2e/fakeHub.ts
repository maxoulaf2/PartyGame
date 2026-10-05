import type { Page } from '@playwright/test';
import type {
    DisplaySnapshot,
    GameId,
    GameMasterRoundIntent,
    GameMasterSnapshot,
    IncidentList,
    RoundId,
} from '../src/shared/contracts';

// The JSON protocol of SignalR ends each message with the 0x1e record separator.
const separator = '\u001e';

const negotiateUrl = /\/hub\/game\/negotiate/;
const hubUrl = /\/hub\/game/;

/** What the fake hub needs to read from a message of the page. */
interface ClientMessage {
    type?: number;
    target?: string;
    invocationId?: string;
    arguments?: unknown[];
}

/** What the fake hub does with an invocation: the result to answer, after `snapshots` if any. */
interface Answer {
    result: object | null;
    snapshots?: object[];
}

type Invocations = Partial<Record<string, (args: unknown[]) => Answer>>;

/** A message the fake hub sends on its own, such as one sent on announcement. */
interface ServerMessage {
    target: string;
    arguments: unknown[];
}

function send(socket: { send(message: string): void }, message: object): void {
    socket.send(JSON.stringify(message) + separator);
}

/**
 * Stands in for the game hub of the TV page, which then receives `snapshot` in answer to its
 * announcement: lets a test show states the shared server cannot reach on demand, such as 20
 * players or no address at all.
 */
export function serveDisplaySnapshot(page: Page, snapshot: DisplaySnapshot): Promise<void> {
    return serveSnapshot(page, 'ReceiveDisplaySnapshot', snapshot);
}

/**
 * Stands in for the game hub of the GM console, which then receives `snapshot` in answer to its
 * announcement, whatever the code: lets a test show states the shared server cannot reach on
 * demand, such as a lobby without any player, or a host on several networks.
 *
 * An address choice is handled as the server does: a candidate is advertised in a new snapshot,
 * anything else is refused. So is a skip of the round in progress, which ends it. `chosen` records
 * every address the console sent, `roundIntents` every intent it sent to the round in progress,
 * which changes nothing, and `skipped` every round it asked to skip. `incidents`, if any, are sent
 * after the snapshot. `resolved` records every decision about a game found saved, which changes
 * nothing, while a check of its media files finds them all back. `returned` records every game it
 * asked to end, which goes back to the lobby when it names the game in progress.
 */
export async function serveGameMasterSnapshot(
    page: Page,
    snapshot: GameMasterSnapshot,
    incidents?: IncidentList,
): Promise<{
    chosen: string[];
    roundIntents: GameMasterRoundIntent[];
    skipped: RoundId[];
    resolved: { savedGameId: GameId; resume: boolean }[];
    returned: GameId[];
}> {
    const resolved: { savedGameId: GameId; resume: boolean }[] = [];
    const chosen: string[] = [];
    const roundIntents: GameMasterRoundIntent[] = [];
    const skipped: RoundId[] = [];
    const returned: GameId[] = [];
    let current = snapshot;
    const announced =
        incidents === undefined ? [] : [{ target: 'ReceiveIncidents', arguments: [incidents] }];
    await serveSnapshot(page, 'ReceiveGameMasterSnapshot', snapshot, announced, {
        ChooseAdvertisedAddress: ([request]) => {
            const { address } = request as { address: string };
            chosen.push(address);
            if (!current.joinAddressCandidates.some((c) => c.address === address)) {
                return { result: { refusal: 'AddressUnknown' } };
            }
            current = { ...current, version: current.version + 1, joinAddress: address };
            return { result: { refusal: null }, snapshots: [current] };
        },
        SendGameMasterRoundIntent: ([intent]) => {
            roundIntents.push(intent as GameMasterRoundIntent);
            return { result: null };
        },
        ResolveSavedGame: ([request]) => {
            resolved.push(request as { savedGameId: GameId; resume: boolean });
            return { result: null };
        },
        CheckSavedGameMedia: () => {
            if (current.savedGame === null) {
                return { result: null };
            }
            current = {
                ...current,
                version: current.version + 1,
                savedGame: { ...current.savedGame, missingMedia: [] },
            };
            return { result: null, snapshots: [current] };
        },
        ShowJoinCode: ([request]) => {
            const { shown } = request as { shown: boolean };
            current = { ...current, version: current.version + 1, joinCodeShown: shown };
            return { result: null, snapshots: [current] };
        },
        SkipRound: ([request]) => {
            const { roundId } = request as { roundId: RoundId };
            skipped.push(roundId);
            if (current.phase !== 'Round' || current.round?.roundId !== roundId) {
                return { result: null };
            }
            current = {
                ...current,
                version: current.version + 1,
                phase: 'BetweenRounds',
                roundView: null,
                roundSkipped: true,
            };
            return { result: null, snapshots: [current] };
        },
        ReturnToLobby: ([request]) => {
            const { gameId } = request as { gameId: GameId };
            returned.push(gameId);
            if (current.phase === 'Lobby' || current.gameId !== gameId) {
                return { result: null };
            }
            current = {
                ...current,
                version: current.version + 1,
                phase: 'Lobby',
                round: null,
                roundView: null,
                ranking: [],
            };
            return { result: null, snapshots: [current] };
        },
    });
    return { chosen, roundIntents, skipped, resolved, returned };
}

async function serveSnapshot(
    page: Page,
    target: string,
    snapshot: object,
    announced: ServerMessage[] = [],
    invocations: Invocations = {},
): Promise<void> {
    await page.route(negotiateUrl, (route) =>
        route.fulfill({
            json: {
                negotiateVersion: 1,
                connectionId: 'fake',
                connectionToken: 'fake',
                availableTransports: [
                    { transport: 'WebSockets', transferFormats: ['Text', 'Binary'] },
                ],
            },
        }),
    );
    await page.routeWebSocket(hubUrl, (socket) => {
        socket.onMessage((data) => {
            for (const frame of String(data).split(separator)) {
                if (frame === '') {
                    continue;
                }
                const message = JSON.parse(frame) as ClientMessage;
                if (message.type === undefined) {
                    send(socket, {}); // handshake accepted
                } else if (message.type === 1 && message.target === 'Announce') {
                    send(socket, {
                        type: 1,
                        target,
                        arguments: [snapshot],
                    });
                    for (const extra of announced) {
                        send(socket, { type: 1, ...extra });
                    }
                    send(socket, {
                        type: 3,
                        invocationId: message.invocationId,
                        result: { refusal: null },
                    });
                } else if (message.type === 1 && message.target !== undefined) {
                    const answer = invocations[message.target]?.(message.arguments ?? []);
                    if (answer === undefined) {
                        continue;
                    }
                    for (const next of answer.snapshots ?? []) {
                        send(socket, { type: 1, target, arguments: [next] });
                    }
                    send(socket, {
                        type: 3,
                        invocationId: message.invocationId,
                        result: answer.result,
                    });
                }
            }
        });
    });
}

/** An invocation the page made of the hub. */
export interface Invocation {
    readonly target: string;
    readonly args: readonly unknown[];
}

/** What a page did with a scripted hub, and how to send it more. */
export interface ScriptedHub {
    /** Every invocation the page made, in order. */
    readonly invocations: Invocation[];
    /** Sends `snapshot` to the page, as the server broadcasts a change. */
    push(snapshot: object): void;
}

/** The invocations a page makes to tell the server what went wrong: answered at once. */
const reports = new Set(['ReportClientError', 'ReportDisplayMediaFailure']);

/**
 * Stands in for the game hub of any page, which receives `snapshot` as `target` once it announces
 * itself or resumes the session of its player, both accepted whatever their message, then every
 * snapshot `push` sends: lets a test send what the shared server never would, such as a snapshot a
 * view cannot render. The reports of the page are recorded with every other invocation.
 */
export async function serveScriptedHub(
    page: Page,
    target: 'ReceiveDisplaySnapshot' | 'ReceiveGameMasterSnapshot' | 'ReceivePlayerSnapshot',
    snapshot: object,
    announced: ServerMessage[] = [],
): Promise<ScriptedHub> {
    const invocations: Invocation[] = [];
    let connected: { send(message: string): void } | null = null;
    await page.route(negotiateUrl, (route) =>
        route.fulfill({
            json: {
                negotiateVersion: 1,
                connectionId: 'fake',
                connectionToken: 'fake',
                availableTransports: [
                    { transport: 'WebSockets', transferFormats: ['Text', 'Binary'] },
                ],
            },
        }),
    );
    await page.routeWebSocket(hubUrl, (socket) => {
        connected = socket;
        socket.onMessage((data) => {
            for (const frame of String(data).split(separator)) {
                if (frame === '') {
                    continue;
                }
                const message = JSON.parse(frame) as ClientMessage;
                if (message.type === undefined) {
                    send(socket, {}); // handshake accepted
                    continue;
                }
                if (message.type !== 1 || message.target === undefined) {
                    continue;
                }
                invocations.push({ target: message.target, args: message.arguments ?? [] });
                if (message.target === 'Announce' || message.target === 'ResumeSession') {
                    send(socket, { type: 1, target, arguments: [snapshot] });
                    for (const extra of announced) {
                        send(socket, { type: 1, ...extra });
                    }
                    const { playerId = null } = snapshot as { playerId?: string };
                    send(socket, {
                        type: 3,
                        invocationId: message.invocationId,
                        result: { refusal: null, playerId },
                    });
                } else if (reports.has(message.target)) {
                    send(socket, { type: 3, invocationId: message.invocationId, result: null });
                }
            }
        });
    });
    return {
        invocations,
        push: (next) => {
            if (connected !== null) {
                send(connected, { type: 1, target, arguments: [next] });
            }
        },
    };
}

/** Makes the game hub unreachable for the page, as when the server is down. */
export async function blockHub(page: Page): Promise<void> {
    await page.route(negotiateUrl, (route) => route.fulfill({ status: 502 }));
}
