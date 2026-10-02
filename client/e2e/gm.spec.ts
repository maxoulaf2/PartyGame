import { expect, test, type Page } from '@playwright/test';
import { gameMasterCodeKey } from '../src/shared/connection/codeStorage.ts';
import { fr } from '../src/shared/i18n/fr.ts';
import { gameMasterCode } from './gameServer.ts';
import { trackExternalRequests } from './localRequests.ts';
import { joinOnNewPhone, uniqueNickname } from './players.ts';

/** Opens the GM console with the right code already remembered, as after a first visit. */
async function openConsole(page: Page): Promise<void> {
    await page.addInitScript(
        ([key, code]) => {
            localStorage.setItem(key, code);
        },
        [gameMasterCodeKey, gameMasterCode] as const,
    );
    await page.goto('/gm/');
    await expect(page.getByRole('heading', { name: fr.gm.consoleTitle })).toBeVisible();
}

function withNickname(text: string, nickname: string): string {
    return text.replace('{nickname}', () => nickname);
}

/** Records the target of every hub message the page receives. */
function trackHubMessages(page: Page): string[] {
    const targets: string[] = [];
    page.on('websocket', (socket) => {
        socket.on('framereceived', ({ payload }) => {
            // The JSON protocol separates messages with the 0x1e record separator.
            for (const message of String(payload).split('\u001e')) {
                const target = /"target":"([^"]+)"/.exec(message)?.[1];
                if (target) {
                    targets.push(target);
                }
            }
        });
    });
    return targets;
}

test('/gm/ asks for the code, refuses a wrong one, then remembers the right one', async ({
    page,
}) => {
    const external = trackExternalRequests(page);
    const received = trackHubMessages(page);
    await page.goto('/gm/');

    const field = page.getByLabel(fr.gm.code.label);
    const submit = page.getByRole('button', { name: fr.gm.code.submit });
    await expect(field).toHaveAttribute('inputmode', 'numeric');
    await field.fill('12345');
    await expect(submit).toBeDisabled();

    await field.fill('000000');
    await expect(submit).toBeEnabled();
    await submit.click();

    await expect(page.getByText(fr.gm.code.invalid)).toBeVisible();
    await expect(field).toHaveValue('');
    await expect(field).toBeFocused();
    expect(received).not.toContain('ReceiveGameMasterSnapshot');

    await field.fill(` ${gameMasterCode} `);
    await submit.click();

    await expect(page.getByRole('heading', { name: fr.gm.consoleTitle })).toBeVisible();
    expect(received).toContain('ReceiveGameMasterSnapshot');

    await page.reload();

    await expect(page.getByRole('heading', { name: fr.gm.consoleTitle })).toBeVisible();
    await expect(page.getByLabel(fr.gm.code.label)).toHaveCount(0);
    expect(external).toEqual([]);
});

test('/gm/ asks for the code again when the remembered one is no longer valid', async ({
    page,
}) => {
    await page.addInitScript((key) => {
        localStorage.setItem(key, '000000');
    }, gameMasterCodeKey);

    await page.goto('/gm/');

    await expect(page.getByText(fr.gm.code.expired)).toBeVisible();
    await expect(page.getByLabel(fr.gm.code.label)).toBeVisible();
    expect(await page.evaluate((key) => localStorage.getItem(key), gameMasterCodeKey)).toBeNull();
});

test('/gm/ lists the players and renames one on every interface', async ({
    page,
    browser,
    baseURL,
}) => {
    const nickname = uniqueNickname('Zoé');
    const renamed = uniqueNickname('Léa');
    const phone = await joinOnNewPhone(browser, baseURL, nickname);
    const display = await (await browser.newContext({ baseURL })).newPage();
    await display.goto('/display/');
    await openConsole(page);

    const players = page.getByRole('list', { name: fr.gm.playerListLabel });
    const row = players.getByRole('listitem').filter({ hasText: nickname });
    await expect(row).toContainText(fr.gm.connected);

    await row.getByRole('button', { name: withNickname(fr.gm.rename.actionFor, nickname) }).click();
    const field = page.getByLabel(withNickname(fr.gm.rename.label, nickname));
    const submit = page.getByRole('button', { name: fr.gm.rename.submit });

    // An invisible character only the server spots: refused, and the field keeps what was typed.
    await field.fill('Zo​é');
    await submit.click();
    await expect(page.getByText(fr.gm.rename.problems.invalid)).toBeVisible();
    await expect(field).toHaveValue('Zo​é');

    await field.fill(renamed);
    await submit.click();

    await expect(field).toHaveCount(0);
    await expect(players.getByText(renamed, { exact: true })).toBeVisible();
    await expect(players.getByText(nickname, { exact: true })).toHaveCount(0);
    await expect(
        display
            .getByRole('list', { name: fr.display.playerListLabel })
            .getByText(renamed, { exact: true }),
    ).toBeVisible();
    await expect(phone.getByText(withNickname(fr.player.registeredAs, renamed))).toBeVisible();

    await phone.context().close();
    await display.context().close();
});

test('/gm/ shows a player whose phone left as disconnected', async ({ page, browser, baseURL }) => {
    const nickname = uniqueNickname('Max');
    const phone = await joinOnNewPhone(browser, baseURL, nickname);
    await openConsole(page);
    const row = page
        .getByRole('list', { name: fr.gm.playerListLabel })
        .getByRole('listitem')
        .filter({ hasText: nickname });
    await expect(row).toContainText(fr.gm.connected);

    await phone.context().close();

    await expect(row).toContainText(fr.gm.disconnected);
});
