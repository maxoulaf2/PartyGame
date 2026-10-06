import { cpSync, mkdtempSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { fileURLToPath } from 'node:url';
import type { Page } from '@playwright/test';
import { fill } from '../src/shared/i18n/fill.ts';
import { fr } from '../src/shared/i18n/fr.ts';
import { expect, test } from './fixtures/table.ts';

// The game master previews a pack of each mode on the TV screen from the lobby, answers included,
// while the phones stay on the lobby.

const previewPacks = mkdtempSync(join(tmpdir(), 'partygame-e2e-preview-'));
for (const [folder, id] of [
    ['./packs', 'soiree'],
    ['./buzzer-packs', 'buzzer'],
    ['./blindtest-packs', 'blindtest'],
    ['./openquestion-packs', 'openquestion'],
] as const) {
    cpSync(fileURLToPath(new URL(`${folder}/${id}`, import.meta.url)), join(previewPacks, id), {
        recursive: true,
    });
}

test.use({ serverPacks: previewPacks });

/** Asks for the preview of the pack titled `title`, and confirms that answers will show. */
async function preview(gm: Page, title: string): Promise<void> {
    await gm
        .getByRole('listitem')
        .filter({ has: gm.getByText(title, { exact: true }) })
        .getByRole('button', { name: fr.gm.preview.action, exact: true })
        .click();
    await gm
        .getByRole('dialog', { name: fr.gm.preview.confirmTitle })
        .getByRole('button', { name: fr.gm.preview.confirm, exact: true })
        .click();
}

function position(number: number, count: number, step: number, steps: number): string {
    return fill(fr.game.previewPosition, { number, count, step, steps });
}

test('the game master previews a pack of each mode on the TV screen, the phones staying on the lobby', async ({
    table,
}) => {
    const { display, gm } = table;
    const [zoe] = table.players;
    await display
        .getByRole('button', { name: fr.display.startAudio })
        .click({ timeout: 2_000 })
        .catch(() => undefined);

    // Quiz: each question revealed, step by step, then round by round.
    await preview(gm, 'Grande soirée');
    await expect(display.getByText(position(1, 2, 1, 3))).toBeVisible();
    await expect(display.getByText(fr.display.previewBanner, { exact: false })).toBeVisible();
    await expect(display.getByText('Combien de pattes a une araignée ?')).toBeVisible();
    await gm.getByRole('button', { name: fr.gm.preview.next, exact: true }).click();
    await expect(display.getByText('Quelle planète est la plus proche du Soleil ?')).toBeVisible();
    await gm.getByRole('button', { name: fr.gm.preview.previous, exact: true }).click();
    await expect(display.getByText(position(1, 2, 1, 3))).toBeVisible();
    await gm.getByLabel(fr.gm.preview.roundLabel).selectOption('2');
    await expect(display.getByText(/Manche 2\/2 · Question 1\//)).toBeVisible();

    // The phones learn nothing of it.
    await expect(zoe.page.getByText(fr.player.waiting)).toBeVisible();
    await expect(zoe.page.getByText('araignée')).toHaveCount(0);

    // Buzzer: the question with its answer.
    await preview(gm, 'Soirée buzzer');
    await expect(display.getByText(position(1, 1, 1, 2))).toBeVisible();
    await expect(display.getByText('Léonard de Vinci')).toBeVisible();

    // Blind test: the track revealed, its excerpt played on demand.
    await preview(gm, 'Blind test');
    await expect(display.getByText("L'Hymne à la joie")).toBeVisible();
    await gm.getByRole('button', { name: fr.gm.preview.playExcerpt, exact: true }).click();
    await expect
        .poll(() =>
            display.locator('audio').evaluate((audio: HTMLAudioElement) => audio.currentTime),
        )
        .toBeGreaterThan(0.5);

    // Open question: the expected answer.
    await preview(gm, 'Questions ouvertes');
    await expect(display.getByText(position(1, 1, 1, 2))).toBeVisible();
    await expect(display.getByText('Léonard de Vinci')).toBeVisible();
    await expect(
        gm.getByRole('button', { name: fr.gm.preview.playExcerpt, exact: true }),
    ).toHaveCount(0);

    // Back to the lobby.
    await gm.getByRole('button', { name: fr.gm.preview.stop, exact: true }).click();
    await expect(display.getByRole('img', { name: fr.display.qrCodeLabel })).toBeVisible();
    await expect(display.getByText(fr.display.previewBanner, { exact: false })).toHaveCount(0);
});
