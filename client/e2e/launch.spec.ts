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

function choicesOf(page: Page) {
    return page.getByRole('list', { name: fr.modes.quiz.choicesLabel }).getByRole('listitem');
}

/**
 * The phone is an answer pad: a button per choice, waiting for the answers to open, and neither
 * the question nor the texts of the choices, read on the TV screen.
 */
async function expectQuestionOnPhone(phone: Page, question: string, progress: string) {
    await expect(phone.getByText(progress)).toBeVisible();
    const buttons = choicesOf(phone).getByRole('button');
    await expect(buttons).toHaveCount(2);
    for (const letter of ['A', 'B']) {
        const button = phone.getByRole('button', {
            name: fill(fr.modes.quiz.player.choiceLabel, { letter }),
        });
        await expect(button).toBeDisabled();
        await expect(button).toHaveText(letter);
    }
    await expect(phone.getByText(fr.modes.quiz.player.presentation)).toBeVisible();
    await expect(phone.getByText(question)).toHaveCount(0);
    await expect(phone.getByText('araignée')).toHaveCount(0);
}

test('the game master starts the game, and every interface presents its first question', async ({
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

    // The first question of the first round is presented on every interface, its correct answer
    // on the console only.
    const question = 'Combien de pattes a une araignée ?';
    const progress = fill(fr.modes.quiz.question, { number: 1, count: 1 });
    await expect(
        page.getByText(fill(fr.game.round, { number: 1, count: playedPack.rounds.length })),
    ).toBeVisible();
    await expect(page.getByRole('heading', { name: playedPack.rounds[0] })).toBeVisible();
    await expect(page.getByRole('heading', { name: question })).toBeVisible();
    await expect(choicesOf(page)).toHaveText(['A 6', `B 8 ${fr.modes.quiz.gm.correct}`]);
    await expect(page.getByRole('button', { name: fr.modes.quiz.gm.openAnswers })).toBeVisible();
    await expect(page.getByRole('button', { name: fr.modes.quiz.gm.skipQuestion })).toBeVisible();
    await expect(start).toHaveCount(0);
    // The pack is fixed: neither the list of packs nor their reload are offered anymore.
    await expect(page.getByRole('radio')).toHaveCount(0);
    await expect(page.getByRole('button', { name: fr.gm.packs.reload })).toHaveCount(0);
    await expect(
        page.getByText(fr.gm.packs.played.replace('{title}', () => playedPack.title)),
    ).toBeVisible();

    await expect(display.getByText(playedPack.rounds[0], { exact: true })).toBeVisible();
    await expect(display.getByText(progress)).toBeVisible();
    await expect(display.getByRole('heading', { name: question })).toBeVisible();
    await expect(choicesOf(display)).toHaveText(['A 6', 'B 8']);

    await expectQuestionOnPhone(phone, question, progress);

    // Registration stays open: a late phone joins during the presentation, and sees the question.
    const latePhone = await joinOnNewPhone(browser, baseURL, lateNickname);
    await expectQuestionOnPhone(latePhone, question, progress);
    await expect(
        page
            .getByRole('list', { name: fr.gm.playerListLabel })
            .getByText(lateNickname, { exact: true }),
    ).toBeVisible();

    for (const other of [display, phone, latePhone]) {
        await expect(other.getByText(fr.modes.quiz.gm.correct)).toHaveCount(0);
    }

    await Promise.all([phone, latePhone, display].map((other) => other.context().close()));
});
