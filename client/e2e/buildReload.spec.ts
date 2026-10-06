import { expect, test, type Page, type WebSocketRoute } from '@playwright/test';
import { reloadTargetKey } from '../src/shared/connection/codeStorage.ts';
import { fr } from '../src/shared/i18n/fr.ts';
import { uniqueNickname } from './players.ts';

/** The build an updated server would serve, unlike the build the tests run. */
const newerBuild = 'e2e-newer-build';

/** Ends each message of the SignalR JSON protocol. */
const recordSeparator = '\u001e';

/**
 * Relays the WebSockets of the page to the server, and lets the test pretend that the server was
 * updated: from then on, the welcome of the server announces another build. Also records what
 * the page sends. Set up before the page opens.
 */
async function relayToUpdatableServer(page: Page) {
    const open = new Set<[WebSocketRoute, WebSocketRoute]>();
    const sent: string[] = [];
    let updated = false;

    const rewrite = (message: string) =>
        message
            .split(recordSeparator)
            .map((record) => {
                if (!updated || !record.includes('"ReceiveWelcome"')) {
                    return record;
                }
                const welcome = JSON.parse(record) as { arguments: [{ buildId: string | null }] };
                welcome.arguments[0].buildId = newerBuild;
                return JSON.stringify(welcome);
            })
            .join(recordSeparator);

    await page.routeWebSocket(/\/hub\/game/, (toPage) => {
        const toServer = toPage.connectToServer();
        open.add([toPage, toServer]);
        toPage.onMessage((message) => {
            sent.push(String(message));
            toServer.send(message);
        });
        toServer.onMessage((message) => {
            toPage.send(typeof message === 'string' ? rewrite(message) : message);
        });
    });

    return {
        sent,
        /** Restarts the server with another build: connections drop, and come back to it. */
        update: async () => {
            updated = true;
            const sockets = [...open];
            open.clear();
            for (const [toPage, toServer] of sockets) {
                await toPage.close();
                await toServer.close();
            }
        },
    };
}

test('a phone left open during an update reloads once, then finds its place back', async ({
    page,
}) => {
    const nickname = uniqueNickname('Léa');
    const server = await relayToUpdatableServer(page);
    let loads = 0;
    page.on('load', () => loads++);
    await page.goto('/');
    await page.getByLabel(fr.player.join.label).fill(nickname);
    await page.getByRole('button', { name: fr.player.join.submit }).click();
    await expect(page.getByText(nickname, { exact: true })).toBeVisible();
    // The build served is the one of the page: nothing to reload.
    expect(loads).toBe(1);

    await server.update();

    // The preview server still serves the same build: the reload cannot fix the gap.
    await expect
        .poll(() => server.sent.filter((message) => message.includes('"ReportStaleBuild"')))
        .toHaveLength(1);
    expect(loads).toBe(2);
    expect(await page.evaluate((key) => sessionStorage.getItem(key), reloadTargetKey)).toBe(
        newerBuild,
    );
    await expect(page.getByText(nickname, { exact: true })).toBeVisible();
    await expect(page.getByLabel(fr.player.join.label)).toHaveCount(0);

    // The page goes on with its build, without reloading in a loop.
    await page.waitForTimeout(2_000);
    expect(loads).toBe(2);
});
