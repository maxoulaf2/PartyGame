import { fileURLToPath } from 'node:url';
import type { Page } from '@playwright/test';
import { fill } from '../src/shared/i18n/fill.ts';
import { fr } from '../src/shared/i18n/fr.ts';
import { expect, test } from './fixtures/table.ts';

// The game master pauses the game during the countdown of a quiz question, then resumes it: the
// countdown stands still meanwhile, every phone shows the pause, and the game goes on where it stood.

/** A single pack, chosen at once, whose first round is a quiz of one question. */
const introPacks = fileURLToPath(new URL('./intro-packs', import.meta.url));

test.use({ serverPacks: introPacks });

async function secondsLeft(page: Page): Promise<number> {
    return Number(await page.getByRole('timer').locator('.seconds').textContent());
}

function choiceButton(phone: Page, letter: string) {
    return phone.getByRole('button', { name: fill(fr.modes.quiz.player.choiceLabel, { letter }) });
}

test('a pause during a quiz question stops its countdown until the game master resumes', async ({
    table,
}) => {
    const { display, gm } = table;
    const [zoe] = table.players;
    await gm.getByRole('button', { name: fr.gm.start.action, exact: true }).click();
    await gm
        .getByRole('dialog', { name: fr.gm.start.confirmTitle })
        .getByRole('button', { name: fr.gm.start.confirm, exact: true })
        .click();
    await gm.getByRole('button', { name: fr.gm.startRound.action }).click();
    await gm.getByRole('button', { name: fr.modes.quiz.gm.showQuestion }).click();
    for (const letter of ['A', 'B']) {
        await gm
            .getByRole('button', { name: fill(fr.modes.quiz.gm.showChoice, { letter }) })
            .click();
    }
    await expect(choiceButton(zoe.page, 'A')).toBeEnabled();

    // Paused: the TV screen shows the pause over the question, the phones have nothing to tap.
    await gm.getByRole('button', { name: fr.gm.pause.pause, exact: true }).click();
    await expect(display.getByText(fr.display.paused, { exact: true })).toBeVisible();
    await expect(gm.getByRole('button', { name: fr.gm.pause.resume })).toBeVisible();
    await expect(gm.getByText(fr.gm.pause.paused)).toBeVisible();
    for (const player of table.players) {
        await expect(player.page.getByText(fr.player.paused)).toBeVisible();
        await expect(player.page.getByRole('button')).toHaveCount(0);
    }

    // The countdown stands still, on a phone reloaded meanwhile as well.
    const left = await secondsLeft(display);
    await display.waitForTimeout(2_000);
    expect(await secondsLeft(display)).toBe(left);
    await zoe.page.reload();
    await expect(zoe.page.getByText(fr.player.paused)).toBeVisible();

    // Resumed: the countdown goes on from where it stood, and Zoé answers.
    await gm.getByRole('button', { name: fr.gm.pause.resume }).click();
    await expect(display.getByText(fr.display.paused, { exact: true })).toHaveCount(0);
    expect(await secondsLeft(display)).toBeGreaterThanOrEqual(left - 1);
    await choiceButton(zoe.page, 'B').click();
    await expect(zoe.page.getByText(fr.modes.quiz.player.recorded)).toBeVisible();
});
