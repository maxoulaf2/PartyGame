import { expect, test, type Page } from '@playwright/test';
import { gameMasterCodeKey } from '../src/shared/connection/codeStorage.ts';
import { fill } from '../src/shared/i18n/fill.ts';
import { fr } from '../src/shared/i18n/fr.ts';
import { gameMasterCode, playedPack } from './gameServer.ts';
import { joinOnNewPhone, uniqueNickname } from './players.ts';

// Starting the game cannot be undone, and every test shares the same server: playwright.config.ts
// runs this file in a project of its own, once all the other tests are done.

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

test('the game master starts the game, and every interface follows its rounds to the end', async ({
    page,
    browser,
    baseURL,
}) => {
    const nickname = uniqueNickname('Zoé');
    const lateNickname = uniqueNickname('Max');
    const phone = await joinOnNewPhone(browser, baseURL, nickname);
    const display = await (await browser.newContext({ baseURL })).newPage();
    await display.goto('/display/');
    await openConsole(page);
    await expect(phone.getByText(fr.player.waiting)).toBeVisible();

    const start = page.getByRole('button', { name: fr.gm.start.action, exact: true });
    const dialog = page.getByRole('dialog', { name: fr.gm.start.confirmTitle });

    // The pack may already be chosen by the tests of the packs: choosing it again changes nothing.
    await page.getByRole('radio', { name: playedPack.title }).check();
    await expect(display.getByText(playedPack.title)).toBeVisible();

    // Cancelling leaves everything in the lobby.
    await start.click();
    await expect(dialog).toBeVisible();
    await dialog.getByRole('button', { name: fr.gm.start.cancel, exact: true }).click();
    await expect(dialog).toHaveCount(0);
    await expect(phone.getByText(fr.player.waiting)).toBeVisible();

    await start.click();
    await dialog.getByRole('button', { name: fr.gm.start.confirm, exact: true }).click();

    // Until the questions are played (US-E08-02), a quiz round finishes as soon as it starts: the
    // game lands at once between the first two rounds of the pack.
    const firstEnded = fill(fr.game.roundEnded, { number: 1, count: playedPack.rounds.length });
    const nextRound = page.getByRole('button', { name: fr.gm.nextRound.action });
    await expect(page.getByText(firstEnded)).toBeVisible();
    await expect(page.getByRole('heading', { name: playedPack.rounds[0] })).toBeVisible();
    await expect(nextRound).toBeEnabled();
    await expect(start).toHaveCount(0);
    // The pack is fixed: neither the list of packs nor their reload are offered anymore.
    await expect(page.getByRole('radio')).toHaveCount(0);
    await expect(page.getByRole('button', { name: fr.gm.packs.reload })).toHaveCount(0);
    await expect(
        page.getByText(fr.gm.packs.played.replace('{title}', () => playedPack.title)),
    ).toBeVisible();
    await expect(display.getByText(firstEnded)).toBeVisible();
    await expect(display.getByText(fr.display.betweenRounds)).toBeVisible();
    await expect(phone.getByText(firstEnded)).toBeVisible();
    await expect(phone.getByText(fr.player.betweenRounds)).toBeVisible();

    // Registration stays open: a late phone joins the started game, and the TV still invites.
    await expect(display.getByRole('img', { name: fr.display.qrCodeLabel })).toBeVisible();
    const latePhone = await joinOnNewPhone(browser, baseURL, lateNickname);
    await expect(latePhone.getByText(fr.player.betweenRounds)).toBeVisible();
    await expect(
        page
            .getByRole('list', { name: fr.gm.playerListLabel })
            .getByText(lateNickname, { exact: true }),
    ).toBeVisible();
    await expect(
        display
            .getByRole('list', { name: fr.display.playerListLabel })
            .getByText(lateNickname, { exact: true }),
    ).toBeVisible();

    // The last round finishes as soon as it starts too: the game is over on every interface.
    await nextRound.click();
    await expect(page.getByRole('heading', { name: fr.game.finished })).toBeVisible();
    await expect(nextRound).toHaveCount(0);
    for (const other of [display, phone, latePhone]) {
        await expect(other.getByText(fr.game.finished, { exact: true })).toBeVisible();
    }

    await Promise.all([phone, latePhone, display].map((other) => other.context().close()));
});
