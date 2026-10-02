import {
    expect,
    test,
    type Browser,
    type Page,
    type TestInfo,
    type WebSocketRoute,
} from '@playwright/test';
import {
    gameMasterCodeKey,
    playerNicknameKey,
    playerTokenKey,
} from '../src/shared/connection/codeStorage.ts';
import { fr } from '../src/shared/i18n/fr.ts';
import { gameMasterCode } from './gameServer.ts';
import { joinOnNewPhone, uniqueNickname } from './players.ts';

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

/** Cuts the page from the server, as a phone going out of Wi-Fi range. */
async function goOffline(page: Page, network: { cut(): Promise<void> }): Promise<void> {
    await page.context().setOffline(true);
    await network.cut();
}

/**
 * Checks the notice of an outage: absent for its first seconds, shown once it lasts longer than
 * 3 s. Called right after the cut.
 */
async function expectNoticeAfterDelay(page: Page): Promise<void> {
    const notice = page.getByText(fr.connection.reconnecting);
    await expect(notice).toBeHidden();
    await page.waitForTimeout(2_000);
    await expect(notice).toBeHidden();
    await expect(notice).toBeVisible({ timeout: 5_000 });
}

test('a phone tells of a long outage, locks the form and keeps the nickname typed', async ({
    page,
}) => {
    const network = await relayWebSockets(page);
    await page.goto('/');
    const field = page.getByLabel(fr.player.join.label);
    const submit = page.getByRole('button', { name: fr.player.join.submit });
    await field.fill('Zoé');
    await expect(submit).toBeEnabled();

    await goOffline(page, network);

    await expect(field).toBeDisabled();
    await expect(submit).toBeDisabled();
    await expectNoticeAfterDelay(page);
    await expect(field).toHaveValue('Zoé');

    await page.context().setOffline(false);

    await expect(page.getByText(fr.connection.reconnecting)).toBeHidden({ timeout: 15_000 });
    await expect(field).toBeEnabled();
    await expect(field).toHaveValue('Zoé');
    await expect(submit).toBeEnabled();
});

test('a phone in the lobby tells of a long outage until it is back', async ({
    browser,
}, testInfo) => {
    const nickname = uniqueNickname('Léa');
    const phone = await newPhone(browser, testInfo);
    const network = await relayWebSockets(phone);
    await join(phone, nickname);

    await goOffline(phone, network);

    await expectNoticeAfterDelay(phone);
    // The last display stays on screen, the notice shifting nothing.
    await expect(phone.getByText(registeredAs(nickname))).toBeVisible();

    await phone.context().setOffline(false);

    await expect(phone.getByText(fr.connection.reconnecting)).toBeHidden({ timeout: 15_000 });
    await expect(phone.getByText(registeredAs(nickname))).toBeVisible();

    await phone.context().close();
});

test('the GM console locks its controls during an outage and unlocks them once back', async ({
    page,
    browser,
    baseURL,
}) => {
    const nickname = uniqueNickname('Tom');
    const player = await joinOnNewPhone(browser, baseURL, nickname);
    await page.addInitScript(
        ([key, code]) => {
            localStorage.setItem(key, code);
        },
        [gameMasterCodeKey, gameMasterCode] as const,
    );
    const network = await relayWebSockets(page);
    await page.goto('/gm/');
    // Unlike the start, enabled whatever the pack chosen by the other tests.
    const reload = page.getByRole('button', { name: fr.gm.packs.reload });
    const rename = page.getByRole('button', {
        name: fr.gm.rename.actionFor.replace('{nickname}', () => nickname),
    });
    await expect(reload).toBeEnabled();
    await expect(rename).toBeEnabled();

    await goOffline(page, network);

    await expect(reload).toBeDisabled();
    await expect(rename).toBeDisabled();
    await expectNoticeAfterDelay(page);
    await expect(page.getByRole('heading', { name: fr.gm.consoleTitle })).toBeVisible();

    await page.context().setOffline(false);

    await expect(page.getByText(fr.connection.reconnecting)).toBeHidden({ timeout: 15_000 });
    await expect(reload).toBeEnabled();
    await expect(rename).toBeEnabled();

    await player.context().close();
});

test('the GM code form keeps the code being typed during an outage', async ({ page }) => {
    const network = await relayWebSockets(page);
    await page.goto('/gm/');
    const field = page.getByLabel(fr.gm.code.label);
    await expect(field).toBeEnabled();
    await field.fill('2468');

    await goOffline(page, network);

    await expect(field).toBeDisabled();
    await expect(field).toHaveValue('2468');

    await page.context().setOffline(false);

    await expect(field).toBeEnabled({ timeout: 15_000 });
    await expect(field).toHaveValue('2468');
});

test('the TV screen keeps its display during a long outage, with a notice', async ({
    browser,
    baseURL,
}) => {
    const display = await (await browser.newContext({ baseURL })).newPage();
    const network = await relayWebSockets(display);
    await display.goto('/display/');
    const invite = display.getByText(fr.display.scanToJoin);
    await expect(invite).toBeVisible();

    await goOffline(display, network);

    await expectNoticeAfterDelay(display);
    await expect(invite).toBeVisible();
    await expect(display.getByRole('heading', { name: fr.app.name })).toBeVisible();

    await display.context().setOffline(false);

    await expect(display.getByText(fr.connection.reconnecting)).toBeHidden({ timeout: 15_000 });
    await expect(invite).toBeVisible();

    await display.context().close();
});
