import { fileURLToPath } from 'node:url';
import { fill } from '../src/shared/i18n/fill.ts';
import { fr } from '../src/shared/i18n/fr.ts';
import { rankText, standingText } from '../src/shared/i18n/rankText.ts';
import type { Page } from '@playwright/test';
import { expect, test } from './fixtures/table.ts';

// The end of a game on a table of its own: the TV screen reveals the podium step by step, and a
// phone tells its rank only once the TV screen has revealed it.

/** A single pack, chosen at once: two quiz rounds of one question each. */
const rankingPacks = fileURLToPath(new URL('./ranking-packs', import.meta.url));

/** The whole reveal lasts 7.5 s: room for it, and for a slow browser. */
const revealTimeout = 15_000;

test.use({ serverPacks: rankingPacks });

async function skipTheOnlyQuestion(gm: Page) {
    await gm.getByRole('button', { name: fr.gm.startRound.action }).click();
    await gm.getByRole('button', { name: fr.modes.quiz.gm.skipQuestion }).click();
    await gm
        .getByRole('dialog')
        .getByRole('button', { name: fr.modes.quiz.gm.skipConfirm.confirm, exact: true })
        .click();
}

test('the podium is revealed step by step, on the TV screen and the phones alike', async ({
    table,
}) => {
    const { display, gm } = table;
    const [zoe] = table.players;
    await gm.getByRole('button', { name: fr.gm.start.action, exact: true }).click();
    await gm
        .getByRole('dialog', { name: fr.gm.start.confirmTitle })
        .getByRole('button', { name: fr.gm.start.confirm, exact: true })
        .click();
    await skipTheOnlyQuestion(gm);
    await gm.getByRole('button', { name: fr.gm.nextRound.action }).click();
    await skipTheOnlyQuestion(gm);

    // The game is finished: the podium is hidden, and the phones wait for the TV screen.
    const first = rankText(fr.game.rank, 1);
    const firstStep = display.getByRole('list', {
        name: fill(fr.game.podiumStepLabel, { rank: first }),
    });
    await expect(display.getByRole('heading', { name: fr.game.finalRanking })).toBeVisible();
    await expect(zoe.page.getByText(fr.player.revealing)).toBeVisible();
    await expect(firstStep).toBeHidden();

    // Then everybody, ex aequo, stands on the first step, which the phone tells at the same time.
    await expect(firstStep.getByRole('listitem')).toHaveCount(3, { timeout: revealTimeout });
    const standing = standingText(fr.game.standing, fr.game.rank, { rank: 1, isTied: true }, 3);
    await expect(zoe.page.getByText(standing, { exact: true })).toBeVisible({ timeout: 2_000 });
    await expect(zoe.page.getByText(fr.player.revealing)).toHaveCount(0);

    // A TV screen reloaded once the reveal is over shows the whole podium at once.
    await display.reload();
    await expect(firstStep.getByRole('listitem')).toHaveCount(3, { timeout: 2_000 });
});
