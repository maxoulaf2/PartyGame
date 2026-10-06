import { fileURLToPath } from 'node:url';
import { countText } from '../src/shared/i18n/countText.ts';
import { fill } from '../src/shared/i18n/fill.ts';
import { fr } from '../src/shared/i18n/fr.ts';
import { expect, test } from './fixtures/table.ts';

// The game master corrects a score between two rounds: the form refuses a negative total, shows
// the old and new totals before confirmation, and the phone of the player shows the new one.

/** A single pack, chosen at once: two quiz rounds of one question each. */
const rankingPacks = fileURLToPath(new URL('./ranking-packs', import.meta.url));

test.use({ serverPacks: rankingPacks });

test('the game master adjusts a score, which the phone of the player shows', async ({ table }) => {
    const { gm } = table;
    const [zoe] = table.players;
    await gm.getByRole('button', { name: fr.gm.start.action, exact: true }).click();
    await gm
        .getByRole('dialog', { name: fr.gm.start.confirmTitle })
        .getByRole('button', { name: fr.gm.start.confirm, exact: true })
        .click();
    await gm.getByRole('button', { name: fr.gm.startRound.action }).click();
    await gm.getByRole('button', { name: fr.modes.quiz.gm.skipQuestion }).click();
    await gm
        .getByRole('dialog')
        .getByRole('button', { name: fr.modes.quiz.gm.skipConfirm.confirm, exact: true })
        .click();
    await expect(zoe.page.getByText(countText(fr.game.points, 0), { exact: true })).toBeVisible();

    await gm
        .getByRole('button', {
            name: fill(fr.gm.adjustScore.actionFor, { nickname: zoe.nickname }),
        })
        .click();
    const amount = gm.getByLabel(fr.gm.adjustScore.amount, { exact: true });
    const confirm = gm.getByRole('button', { name: fr.gm.adjustScore.submit, exact: true });

    // Below zero: refused by the form.
    await gm.getByText(fr.gm.adjustScore.modes.remove, { exact: true }).click();
    await amount.fill('200');
    await expect(gm.getByText(fr.gm.adjustScore.negative)).toBeVisible();
    await expect(confirm).toBeDisabled();

    // A bonus: the totals before and after, then the phone shows the new one.
    await gm.getByText(fr.gm.adjustScore.modes.add, { exact: true }).click();
    await amount.fill('500');
    await expect(gm.getByText(fill(fr.gm.adjustScore.preview, { old: 0, new: 500 }))).toBeVisible();
    await confirm.click();
    await expect(zoe.page.getByText(countText(fr.game.points, 500), { exact: true })).toBeVisible();
    const players = gm.getByRole('list', { name: fr.gm.playerListLabel });
    await expect(players.getByText(countText(fr.gm.score, 500), { exact: true })).toBeVisible();
    await expect(amount).toHaveCount(0);
});
