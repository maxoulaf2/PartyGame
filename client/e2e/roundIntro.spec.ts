import { fileURLToPath } from 'node:url';
import { fill } from '../src/shared/i18n/fill.ts';
import { fr } from '../src/shared/i18n/fr.ts';
import { expect, test } from './fixtures/table.ts';

// Two rounds of different modes on a table of their own: each one is announced on every screen,
// with the rule of its mode, before the game master starts it.

/** A single pack, chosen at once: a quiz round, then a buzzer round its author tells of. */
const introPacks = fileURLToPath(new URL('./intro-packs', import.meta.url));

const quizQuestion = 'Combien de pattes a une araignée ?';
const description = 'Dernière ligne droite : le premier qui buzze a la main, à vous de jouer !';

test.use({ serverPacks: introPacks });

test('each round is announced with the rule of its mode, then started by the game master', async ({
    table,
}) => {
    const { display, gm } = table;
    const [zoe] = table.players;
    await gm.getByRole('button', { name: fr.gm.start.action, exact: true }).click();
    await gm
        .getByRole('dialog', { name: fr.gm.start.confirmTitle })
        .getByRole('button', { name: fr.gm.start.confirm, exact: true })
        .click();

    // The first round is announced: its number, title, mode and rule on the TV screen, its title
    // and rule on the phones, with nothing to tap, and nothing of its question anywhere.
    await expect(display.getByText(fill(fr.game.round, { number: 1, count: 2 }))).toBeVisible();
    await expect(display.getByRole('heading', { name: 'Échauffement' })).toBeVisible();
    await expect(display.getByText(fr.modes.quiz.name, { exact: true })).toBeVisible();
    await expect(display.getByText(fr.modes.quiz.rule)).toBeVisible();
    for (const player of table.players) {
        await expect(player.page.getByRole('heading', { name: 'Échauffement' })).toBeVisible();
        await expect(player.page.getByText(fr.modes.quiz.rule)).toBeVisible();
        await expect(player.page.getByRole('button')).toHaveCount(0);
    }
    await expect(gm.getByText(fr.modes.quiz.rule)).toBeVisible();
    await expect(gm.getByText(quizQuestion)).toHaveCount(0);

    // A phone reloaded during the introduction finds it again.
    await zoe.page.reload();
    await expect(zoe.page.getByText(fr.modes.quiz.rule)).toBeVisible();

    // Started, the round plays its mode: the question reaches the console.
    await gm.getByRole('button', { name: fr.gm.startRound.action }).click();
    await expect(gm.getByRole('heading', { name: quizQuestion })).toBeVisible();
    await expect(display.getByText(fr.modes.quiz.rule)).toHaveCount(0);

    // Its only question skipped, the round ends, and the game master announces the next one.
    await gm.getByRole('button', { name: fr.modes.quiz.gm.skipQuestion }).click();
    await gm
        .getByRole('dialog')
        .getByRole('button', { name: fr.modes.quiz.gm.skipConfirm.confirm, exact: true })
        .click();
    await gm.getByRole('button', { name: fr.gm.nextRound.action }).click();

    // The second round, of another mode, comes with its own rule and the words of its author.
    await expect(display.getByText(fill(fr.game.round, { number: 2, count: 2 }))).toBeVisible();
    await expect(display.getByRole('heading', { name: 'Le plus rapide' })).toBeVisible();
    await expect(display.getByText(fr.modes.buzzer.name, { exact: true })).toBeVisible();
    await expect(display.getByText(fr.modes.buzzer.rule)).toBeVisible();
    await expect(display.getByText(description)).toBeVisible();
    await expect(zoe.page.getByText(fr.modes.buzzer.rule)).toBeVisible();
    await expect(gm.getByText(description)).toBeVisible();

    await gm.getByRole('button', { name: fr.gm.startRound.action }).click();
    await expect(
        display.getByText(fill(fr.modes.buzzer.display.upcoming, { number: 1 }), { exact: true }),
    ).toBeVisible();
    await expect(
        zoe.page.getByRole('button', { name: fr.buzzer.closed, exact: true }),
    ).toBeDisabled();
});
