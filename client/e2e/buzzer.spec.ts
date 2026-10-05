import { fileURLToPath } from 'node:url';
import type { Page } from '@playwright/test';
import { fill } from '../src/shared/i18n/fill.ts';
import { fr } from '../src/shared/i18n/fr.ts';
import { expect, test } from './fixtures/table.ts';

// A round of buzzer questions on a table of its own: the buzzer reacts on pointerdown, on the
// iPhone under WebKit as on the Pixels under Chromium, and every screen names who has the hand.

/** A single pack, chosen at once: a round of two buzzer questions. */
const buzzerPacks = fileURLToPath(new URL('./buzzer-packs', import.meta.url));

const question = 'Qui a peint La Joconde ?';

/** Long enough for a phone to find a server back: its attempts are up to 10 s apart. */
const reconnectTimeout = 30_000;

test.use({ serverPacks: buzzerPacks });

async function startGame(gm: Page): Promise<void> {
    await gm.getByRole('button', { name: fr.gm.start.action, exact: true }).click();
    await gm
        .getByRole('dialog', { name: fr.gm.start.confirmTitle })
        .getByRole('button', { name: fr.gm.start.confirm, exact: true })
        .click();
}

function buzzer(phone: Page, state: keyof typeof fr.buzzer) {
    return phone.getByRole('button', { name: fr.buzzer[state], exact: true });
}

test('the first player to press has the hand, on every screen', async ({ table }) => {
    const { display, gm } = table;
    const [zoe, max, lea] = table.players;
    await startGame(gm);

    // The question is announced: the console alone shows it, with its answer.
    await expect(
        display.getByText(fill(fr.modes.buzzer.display.upcoming, { number: 1 }), { exact: true }),
    ).toBeVisible();
    await expect(display.getByText(question)).toHaveCount(0);
    await expect(gm.getByText(question)).toBeVisible();
    await expect(
        gm.getByText(fill(fr.modes.buzzer.gm.answer, { answer: 'Léonard de Vinci' })),
    ).toBeVisible();
    for (const player of table.players) {
        await expect(buzzer(player.page, 'closed')).toBeDisabled();
    }

    // The game master asks it: it shows on the TV screen, and the buzzer opens on every phone.
    await gm.getByRole('button', { name: fr.modes.buzzer.gm.askQuestion }).click();
    await expect(display.getByRole('heading', { name: question })).toBeVisible();
    for (const player of table.players) {
        await expect(buzzer(player.page, 'open')).toBeEnabled();
    }

    // Zoé presses on her iPhone, whose Wi-Fi dropped silently: her buzz waits, shown as sent.
    await zoe.network.drop();
    await buzzer(zoe.page, 'open').tap();
    await expect(buzzer(zoe.page, 'sent')).toBeVisible();

    // Max presses on his Pixel: a tap, whose pointerdown alone buzzes. He has the hand.
    await buzzer(max.page, 'open').tap();
    const hasHand = fill(fr.modes.buzzer.hasHand, { nickname: max.nickname });
    await expect(buzzer(max.page, 'won')).toBeVisible();
    await expect(display.getByText(hasHand)).toBeVisible();
    await expect(gm.getByText(hasHand)).toBeVisible();
    await expect(buzzer(lea.page, 'lost')).toBeDisabled();
    await expect(lea.page.getByText(hasHand)).toBeVisible();

    // Zoé's network comes back: her buzz arrives too late, and her phone shows who has the hand.
    await zoe.network.cut();
    await expect(buzzer(zoe.page, 'lost')).toBeVisible({ timeout: reconnectTimeout });
    await expect(zoe.page.getByText(hasHand)).toBeVisible();

    // The TV screen reloaded finds the same state.
    await display.reload();
    await expect(display.getByText(hasHand)).toBeVisible();
    await expect(display.getByRole('heading', { name: question })).toBeVisible();
});
