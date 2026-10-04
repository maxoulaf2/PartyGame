import type { BrowserContext, Page, WebSocketRoute } from '@playwright/test';

/** The network of a page whose WebSockets the test relays to the server, and can break. */
export interface RelayedNetwork {
    /**
     * Closes the WebSockets of the page on both sides, as a phone that knows its network is gone:
     * it reconnects as soon as it can.
     */
    cut(): Promise<void>;
    /**
     * Closes them on the server side only, the page believing it is still connected: what it sends
     * is lost, what the server would send never comes. A phone whose Wi-Fi drops silently. `cut`
     * then plays the return of the network.
     */
    drop(): Promise<void>;
}

/**
 * Relays the WebSockets of the hub to the server, so that the test can break them: offline
 * emulation alone leaves an open WebSocket untouched on WebKit. Set up before the page opens.
 */
export async function relayWebSockets(target: Page | BrowserContext): Promise<RelayedNetwork> {
    const open = new Set<{ toPage: WebSocketRoute; toServer: WebSocketRoute; dropped: boolean }>();
    await target.routeWebSocket(/\/hub\/game/, (toPage) => {
        const relay = { toPage, toServer: toPage.connectToServer(), dropped: false };
        // Relayed by hand rather than by default, so that a dropped relay forwards nothing: neither
        // a message nor the closing of the other side.
        relay.toServer.onMessage((message) => {
            if (!relay.dropped) {
                toPage.send(message);
            }
        });
        toPage.onMessage((message) => {
            if (!relay.dropped) {
                relay.toServer.send(message);
            }
        });
        relay.toServer.onClose(() => {
            if (!relay.dropped) {
                void close(toPage);
            }
        });
        toPage.onClose(() => void close(relay.toServer));
        open.add(relay);
    });
    return {
        cut: async () => {
            const relays = [...open];
            open.clear();
            for (const { toPage, toServer } of relays) {
                await close(toPage);
                await close(toServer);
            }
        },
        drop: async () => {
            for (const relay of open) {
                relay.dropped = true;
                await close(relay.toServer);
            }
        },
    };
}

/** Closes one side of a relay, which may be closed already. */
async function close(side: WebSocketRoute): Promise<void> {
    try {
        await side.close();
    } catch {
        // Closed already, by the other side or the page.
    }
}

/** Cuts the page from the server, as a phone going out of Wi-Fi range. */
export async function goOffline(page: Page, network: RelayedNetwork): Promise<void> {
    await page.context().setOffline(true);
    await network.cut();
}
