import {
    expect,
    test,
    type Browser,
    type Page,
    type TestInfo,
    type WebSocketRoute,
} from '@playwright/test';
import { playerNicknameKey, playerTokenKey } from '../src/shared/connection/codeStorage.ts';
import { fr } from '../src/shared/i18n/fr.ts';
import { uniqueNickname } from './players.ts';

/** A phone of the model of the project, with storage of its own. */
async function newPhone(browser: Browser, testInfo: TestInfo): Promise<Page> {
    const { viewport, userAgent, deviceScaleFactor, isMobile, hasTouch, baseURL } =
        testInfo.project.use;
    const context = await browser.newContext({
        viewport,
        userAgent,
        deviceScaleFactor,
        isMobile,
        hasTouch,
        baseURL,
    });
    return context.newPage();
}

async function join(phone: Page, nickname: string): Promise<void> {
    await phone.goto('/');
    await phone.getByLabel(fr.player.join.label).fill(nickname);
    await phone.getByRole('button', { name: fr.player.join.submit }).click();
    await expect(phone.getByText(registeredAs(nickname))).toBeVisible();
}

function registeredAs(nickname: string): string {
    return fr.player.registeredAs.replace('{nickname}', () => nickname);
}

function tokenOf(phone: Page): Promise<string | null> {
    return phone.evaluate((key) => localStorage.getItem(key), playerTokenKey);
}

/**
 * Relays the WebSockets of the page to the server, so that the test can cut them: offline
 * emulation alone leaves an open WebSocket untouched on WebKit. Set up before the page opens.
 */
async function relayWebSockets(page: Page): Promise<{ cut(): Promise<void> }> {
    const open = new Set<[WebSocketRoute, WebSocketRoute]>();
    await page.routeWebSocket(/\/hub\/game/, (socket) => {
        open.add([socket, socket.connectToServer()]);
    });
    return {
        cut: async () => {
            const sockets = [...open];
            open.clear();
            for (const [toPage, toServer] of sockets) {
                await toPage.close();
                await toServer.close();
            }
        },
    };
}

/** The entry of a player in the list of the TV screen. */
function onDisplay(display: Page, nickname: string) {
    return display
        .getByRole('list', { name: fr.display.playerListLabel })
        .getByRole('listitem')
        .filter({ hasText: nickname });
}

test('a reloaded phone gets back to the lobby without the form', async ({ browser }, testInfo) => {
    const nickname = uniqueNickname('Zoé');
    const phone = await newPhone(browser, testInfo);
    await join(phone, nickname);
    const token = await tokenOf(phone);

    await phone.reload();

    await expect(phone.getByText(registeredAs(nickname))).toBeVisible();
    await expect(phone.getByText(fr.player.waiting)).toBeVisible();
    await expect(phone.getByLabel(fr.player.join.label)).toHaveCount(0);
    expect(await tokenOf(phone)).toBe(token);

    await phone.context().close();
});

test('a phone cut from the network comes back by itself, and the TV screen follows', async ({
    browser,
    baseURL,
}, testInfo) => {
    const nickname = uniqueNickname('Max');
    const display = await (await browser.newContext({ baseURL })).newPage();
    await display.goto('/display/');
    const phone = await newPhone(browser, testInfo);
    const network = await relayWebSockets(phone);
    await join(phone, nickname);
    const entry = onDisplay(display, nickname);
    await expect(entry).toBeVisible();
    await expect(entry).not.toContainText(fr.display.disconnected);

    await phone.context().setOffline(true);
    await network.cut();
    await expect(entry).toContainText(fr.display.disconnected);
    // Attempts fail while offline, for longer than the first delays of the retries.
    await phone.waitForTimeout(5_000);
    await expect(phone.getByText(registeredAs(nickname))).toBeVisible();

    await phone.context().setOffline(false);

    await expect(entry).not.toContainText(fr.display.disconnected);
    await expect(phone.getByText(registeredAs(nickname))).toBeVisible();
    await expect(phone.getByLabel(fr.player.join.label)).toHaveCount(0);

    await Promise.all([phone, display].map((page) => page.context().close()));
});

test('a phone with a token the server does not know registers again, nickname filled', async ({
    page,
}) => {
    await page.addInitScript(
        ([tokenKey, nicknameKey]) => {
            // Only once: the page must be free to forget the token.
            if (sessionStorage.getItem('seeded') === null) {
                sessionStorage.setItem('seeded', 'yes');
                localStorage.setItem(tokenKey, 'token-of-another-evening');
                localStorage.setItem(nicknameKey, 'Zoé');
            }
        },
        [playerTokenKey, playerNicknameKey] as const,
    );

    await page.goto('/');

    await expect(page.getByLabel(fr.player.join.label)).toHaveValue('Zoé');
    await expect.poll(() => tokenOf(page)).toBeNull();
});
