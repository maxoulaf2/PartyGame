import type { Page } from '@playwright/test';
import type { DisplaySnapshot, GameMasterSnapshot } from '../src/shared/contracts';

// The JSON protocol of SignalR ends each message with the 0x1e record separator.
const separator = '\u001e';

const negotiateUrl = /\/hub\/game\/negotiate/;
const hubUrl = /\/hub\/game/;

/** What the fake hub needs to read from a message of the page. */
interface ClientMessage {
    type?: number;
    target?: string;
    invocationId?: string;
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
 * demand, such as a lobby without any player.
 */
export function serveGameMasterSnapshot(page: Page, snapshot: GameMasterSnapshot): Promise<void> {
    return serveSnapshot(page, 'ReceiveGameMasterSnapshot', snapshot);
}

async function serveSnapshot(page: Page, target: string, snapshot: object): Promise<void> {
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
                    send(socket, {
                        type: 3,
                        invocationId: message.invocationId,
                        result: { refusal: null },
                    });
                }
            }
        });
    });
}

/** Makes the game hub unreachable for the page, as when the server is down. */
export async function blockHub(page: Page): Promise<void> {
    await page.route(negotiateUrl, (route) => route.fulfill({ status: 502 }));
}
