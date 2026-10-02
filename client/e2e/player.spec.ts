import { expect, test, type Browser, type Page, type TestInfo } from '@playwright/test';
import { playerNicknameKey, playerTokenKey } from '../src/shared/connection/codeStorage.ts';
import { fr } from '../src/shared/i18n/fr.ts';
import { trackExternalRequests } from './localRequests.ts';
import { uniqueNickname } from './players.ts';

/** Records every message the page receives on its WebSocket, as raw JSON. */
function trackHubFrames(page: Page): string[] {
    const frames: string[] = [];
    page.on('websocket', (socket) => {
        socket.on('framereceived', ({ payload }) => {
            // The JSON protocol separates messages with the 0x1e record separator.
            frames.push(
                ...String(payload)
                    .split('\u001e')
                    .filter((message) => message !== ''),
            );
        });
    });
    return frames;
}

/** Another phone of the same model, with storage of its own. */
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

async function join(page: Page, nickname: string): Promise<void> {
    await page.getByLabel(fr.player.join.label).fill(nickname);
    await page.getByRole('button', { name: fr.player.join.submit }).click();
}

function registeredAs(nickname: string): string {
    return fr.player.registeredAs.replace('{nickname}', () => nickname);
}

test('player page asks for a nickname without external requests', async ({ page }) => {
    const external = trackExternalRequests(page);

    await page.goto('/');

    await expect(page.getByRole('heading', { name: fr.player.join.title })).toBeVisible();
    await expect(page.getByLabel(fr.player.join.label)).toHaveValue('');
    await expect(page.getByRole('button', { name: fr.player.join.submit })).toBeDisabled();
    expect(external).toEqual([]);
});

test('player page cannot be zoomed, even by focusing the nickname field', async ({ page }) => {
    await page.goto('/');

    const viewport = await page.locator('meta[name="viewport"]').getAttribute('content');
    expect(viewport).toContain('user-scalable=no');
    expect(viewport).toContain('maximum-scale=1');
    const touchAction = await page.evaluate(
        () => getComputedStyle(document.documentElement).touchAction,
    );
    expect(touchAction).toBe('manipulation');

    const field = page.getByLabel(fr.player.join.label);
    const button = page.getByRole('button', { name: fr.player.join.submit });
    const fontSize = await field.evaluate((input) => parseFloat(getComputedStyle(input).fontSize));
    expect(fontSize).toBeGreaterThanOrEqual(16);
    for (const target of [field, button]) {
        expect((await target.boundingBox())?.height).toBeGreaterThanOrEqual(48);
        expect(await target.evaluate((element) => getComputedStyle(element).touchAction)).toBe(
            'manipulation',
        );
    }
});

test('three phones join, each with its own nickname and token', async ({ browser }, testInfo) => {
    const players = await Promise.all(
        ['Zoé', 'Max', 'Léa'].map(async (name) => {
            const phone = await newPhone(browser, testInfo);
            return { phone, nickname: uniqueNickname(name), frames: trackHubFrames(phone) };
        }),
    );
    const phones = players.map(({ phone }) => phone);
    const frames = players.map((player) => player.frames);

    for (const { phone, nickname } of players) {
        await phone.goto('/');
        await join(phone, `  ${nickname} `);
    }

    const tokens: string[] = [];
    for (const { phone, nickname } of players) {
        await expect(phone.getByText(registeredAs(nickname))).toBeVisible();
        await expect(phone.getByText(fr.player.waiting)).toBeVisible();
        const [token, remembered] = await phone.evaluate(
            ([tokenKey, nicknameKey]) => [
                localStorage.getItem(tokenKey ?? ''),
                localStorage.getItem(nicknameKey ?? ''),
            ],
            [playerTokenKey, playerNicknameKey],
        );
        expect(token).toMatch(/^[A-Za-z0-9_-]{22}$/);
        expect(remembered).toBe(nickname);
        tokens.push(token ?? '');
    }
    expect(new Set(tokens).size).toBe(3);

    // A token travels once, in the answer to its own registration, and never in a snapshot.
    for (const [index, received] of frames.entries()) {
        const snapshots = received.filter((frame) => frame.includes('"target":'));
        expect(snapshots.length).toBeGreaterThan(0);
        for (const token of tokens) {
            for (const snapshot of snapshots) {
                expect(snapshot).not.toContain(token);
            }
        }
        const others = tokens.filter((_, other) => other !== index);
        for (const frame of received) {
            for (const token of others) {
                expect(frame).not.toContain(token);
            }
        }
    }

    await Promise.all(phones.map((phone) => phone.context().close()));
});

test('a nickname already taken, ignoring case and accents, is refused', async ({
    page,
    browser,
}, testInfo) => {
    const suffix = Math.random().toString(36).slice(2, 7);
    const first = await newPhone(browser, testInfo);
    await first.goto('/');
    await join(first, `Zoé ${suffix}`);
    await expect(first.getByText(registeredAs(`Zoé ${suffix}`))).toBeVisible();

    await page.goto('/');
    const field = page.getByLabel(fr.player.join.label);
    await join(page, `ZOE ${suffix}`);

    await expect(page.getByText(fr.player.join.problems.taken)).toBeVisible();
    await expect(field).toHaveValue(`ZOE ${suffix}`);
    await expect(field).toBeFocused();
    expect(await page.evaluate((key) => localStorage.getItem(key), playerTokenKey)).toBeNull();

    // Another nickname clears the message and joins.
    await field.fill(`Zoé 2 ${suffix}`);
    await expect(page.getByText(fr.player.join.problems.taken)).toHaveCount(0);
    await page.getByRole('button', { name: fr.player.join.submit }).click();
    await expect(page.getByText(registeredAs(`Zoé 2 ${suffix}`))).toBeVisible();

    await first.context().close();
});

test('a nickname too long is flagged before sending', async ({ page }) => {
    await page.goto('/');
    const field = page.getByLabel(fr.player.join.label);

    await field.fill('Seventeen chars!!');

    await expect(page.getByText(fr.player.join.problems.tooLong)).toBeVisible();
    await expect(page.getByRole('button', { name: fr.player.join.submit })).toBeDisabled();
    await expect(field).toHaveValue('Seventeen chars!!');
});

test('a phone that joined before gets its last nickname back in the form', async ({ page }) => {
    await page.addInitScript((key) => {
        localStorage.setItem(key, 'Zoé');
    }, playerNicknameKey);

    await page.goto('/');

    await expect(page.getByLabel(fr.player.join.label)).toHaveValue('Zoé');
});
