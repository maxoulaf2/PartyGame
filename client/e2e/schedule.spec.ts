import { fileURLToPath } from 'node:url';
import { fill } from '../src/shared/i18n/fill.ts';
import { fr } from '../src/shared/i18n/fr.ts';
import { expect, test } from './fixtures/table.ts';

// The game master changes the programme during the introduction of the first round: the last
// round moves up, the second one is withdrawn. The TV counts the rounds left without learning
// them, the game master skips the first round with the button always offered, and the next round
// announced is the one moved up.

/** A single pack, chosen at once: three quiz rounds of one question each. */
const schedulePacks = fileURLToPath(new URL('./schedule-packs', import.meta.url));

test.use({ serverPacks: schedulePacks });

test('the game master reorders the programme, withdraws a round and skips one', async ({
    table,
}) => {
    const { gm, display } = table;
    await gm.getByRole('button', { name: fr.gm.start.action, exact: true }).click();
    await gm
        .getByRole('dialog', { name: fr.gm.start.confirmTitle })
        .getByRole('button', { name: fr.gm.start.confirm, exact: true })
        .click();
    await expect(display.getByText(fill(fr.game.round, { number: 1, count: 3 }))).toBeVisible();

    await gm.getByText(fr.gm.schedule.title, { exact: true }).click();
    await gm.getByRole('button', { name: fill(fr.gm.schedule.upFor, { title: 'Finale' }) }).click();
    await gm
        .getByRole('button', { name: fill(fr.gm.schedule.withdrawFor, { title: 'Intermède' }) })
        .click();
    await expect(
        gm.getByRole('button', { name: fill(fr.gm.schedule.putBackFor, { title: 'Intermède' }) }),
    ).toBeVisible();
    await expect(display.getByText(fill(fr.game.round, { number: 1, count: 2 }))).toBeVisible();

    // Skipped with the button offered for any round, not only a failing one.
    await gm.getByRole('button', { name: fr.gm.startRound.action }).click();
    await gm.getByRole('button', { name: fr.gm.skipRound.action, exact: true }).click();
    await gm
        .getByRole('dialog', { name: fr.gm.skipRound.confirmTitle })
        .getByRole('button', { name: fr.gm.skipRound.confirm, exact: true })
        .click();
    await expect(
        gm.getByText(fill(fr.gm.nextRound.upcoming, { number: 2, count: 2, title: 'Finale' })),
    ).toBeVisible();

    await gm.getByRole('button', { name: fr.gm.nextRound.action, exact: true }).click();
    await expect(display.getByText(fill(fr.game.round, { number: 2, count: 2 }))).toBeVisible();
    await expect(display.getByRole('heading', { name: 'Finale' })).toBeVisible();
    await expect(display.getByText('Intermède')).toHaveCount(0);
    for (const status of [
        fr.gm.schedule.status.Skipped,
        fr.gm.schedule.status.Current,
        fr.gm.schedule.status.Withdrawn,
    ]) {
        await expect(gm.getByText(status, { exact: true })).toBeVisible();
    }
});
