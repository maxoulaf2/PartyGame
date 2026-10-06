import { mkdtempSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { fileURLToPath } from 'node:url';
import type { Page } from '@playwright/test';
import { fill } from '../src/shared/i18n/fill.ts';
import { fr } from '../src/shared/i18n/fr.ts';
import { expect, test } from './fixtures/table.ts';
import { zipFolder } from './zip.ts';

// A blind test round on a table of its own: the TV screen plays the excerpt when the server
// decides, pauses it as a player gets the hand, and finds it back where it was after a reload.

/** A single pack, chosen at once: a round of two tracks, the second from 9.6 s into its file. */
const blindTestPacks = fileURLToPath(new URL('./blindtest-packs', import.meta.url));

test.use({ serverPacks: blindTestPacks });

async function startGame(gm: Page): Promise<void> {
    await gm.getByRole('button', { name: fr.gm.start.action, exact: true }).click();
    await gm
        .getByRole('dialog', { name: fr.gm.start.confirmTitle })
        .getByRole('button', { name: fr.gm.start.confirm, exact: true })
        .click();
}

/** Clicks « Démarrer » if the browser locks the audio of the TV screen. */
async function unlockAudio(display: Page): Promise<void> {
    await display
        .getByRole('button', { name: fr.display.startAudio })
        .click({ timeout: 2_000 })
        .catch(() => undefined);
}

/** What the single audio element of the TV screen does. */
function audioOf(display: Page) {
    return display.locator('audio').evaluate((audio: HTMLAudioElement) => ({
        src: audio.src,
        paused: audio.paused,
        currentTime: audio.currentTime,
    }));
}

function buzzer(phone: Page, state: 'closed' | 'open' | 'won' | 'lost' | 'blocked') {
    const labels: Record<string, string> = { ...fr.buzzer, ...fr.modes.blindtest.buzzer };
    return phone.getByRole('button', { name: labels[state], exact: true });
}

function track(number: number) {
    return fill(fr.modes.blindtest.track, { number, count: 2 });
}

test('the music plays on the TV screen alone, and pauses as a player gets the hand', async ({
    table,
}) => {
    const { display, gm } = table;
    const [zoe, max, lea] = table.players;
    await unlockAudio(display);
    await startGame(gm);

    // The track is announced: preloaded on the TV screen, which tells nothing of it.
    await expect(display.getByRole('heading', { name: track(1) })).toBeVisible();
    await expect(
        gm.getByText(fill(fr.modes.blindtest.gm.title, { title: "L'Hymne à la joie" })),
    ).toBeVisible();
    await expect(display.getByText('Hymne')).toHaveCount(0);
    const announced = await audioOf(display);
    expect(announced.src).toContain('/media/');
    expect(announced.paused).toBe(true);
    for (const player of table.players) {
        await expect(buzzer(player.page, 'closed')).toBeDisabled();
    }

    // The game master plays it: the music starts, and the buzzers open.
    await gm.getByRole('button', { name: fr.modes.blindtest.gm.play }).click();
    for (const player of table.players) {
        await expect(buzzer(player.page, 'open')).toBeEnabled();
    }
    await expect.poll(async () => (await audioOf(display)).currentTime).toBeGreaterThan(0.5);
    expect((await audioOf(display)).paused).toBe(false);

    // Max buzzes: he has the hand on every screen, and the music stops.
    await buzzer(max.page, 'open').tap();
    const hasHand = fill(fr.modes.blindtest.hasHand, { nickname: max.nickname });
    await expect(buzzer(max.page, 'won')).toBeVisible();
    await expect(display.getByText(hasHand)).toBeVisible();
    await expect(gm.getByText(hasHand)).toBeVisible();
    await expect(lea.page.getByText(hasHand)).toBeVisible();
    await expect(buzzer(zoe.page, 'lost')).toBeDisabled();
    await expect.poll(async () => (await audioOf(display)).paused).toBe(true);
    const paused = (await audioOf(display)).currentTime;

    // The TV screen reloaded stays paused where the music stopped.
    await display.reload();
    await unlockAudio(display);
    await expect(display.getByText(hasHand)).toBeVisible();
    await expect.poll(async () => (await audioOf(display)).currentTime).toBeCloseTo(paused, 0);
    expect((await audioOf(display)).paused).toBe(true);

    // The phones never play anything.
    for (const player of table.players) {
        await expect(player.page.locator('audio')).toHaveCount(0);
    }
});

/** The game master ticks what the player who has the hand found, then validates. */
async function judge(gm: Page, ...verdicts: string[]): Promise<void> {
    for (const verdict of verdicts) {
        await gm.getByRole('button', { name: verdict, exact: true }).click();
    }
    await gm.getByRole('button', { name: fr.modes.blindtest.gm.validate, exact: true }).click();
}

test.describe('a pack shared as a zip', () => {
    // The same pack, as the only zip of its pack directory: the server extracts it, then serves its
    // excerpts as those of a folder.
    const zipped = mkdtempSync(join(tmpdir(), 'partygame-e2e-zip-'));
    zipFolder(join(blindTestPacks, 'blindtest'), join(zipped, 'blindtest.zip'));
    test.use({ serverPacks: zipped });

    test('its excerpt plays on the TV screen', async ({ table }) => {
        const { display, gm } = table;
        await unlockAudio(display);
        await startGame(gm);
        await expect(display.getByRole('heading', { name: track(1) })).toBeVisible();

        await gm.getByRole('button', { name: fr.modes.blindtest.gm.play }).click();

        await expect.poll(async () => (await audioOf(display)).currentTime).toBeGreaterThan(0.5);
        expect((await audioOf(display)).paused).toBe(false);
    });
});

test('a round of two tracks: found by two players, then revealed by the game master', async ({
    table,
}) => {
    const { display, gm } = table;
    const [zoe, max, lea] = table.players;
    await unlockAudio(display);
    await startGame(gm);
    await gm.getByRole('button', { name: fr.modes.blindtest.gm.play }).click();

    // Max gives the title: the music resumes for the others, and he may not buzz again.
    await buzzer(max.page, 'open').tap();
    await expect(buzzer(max.page, 'won')).toBeVisible();
    await expect.poll(async () => (await audioOf(display)).paused).toBe(true);
    const paused = (await audioOf(display)).currentTime;
    await judge(gm, fr.modes.blindtest.gm.titleFound);
    const titleFound = fill(fr.modes.blindtest.titleFoundBy, { nickname: max.nickname });
    await expect(display.getByText(titleFound)).toBeVisible();
    await expect(display.getByText('Hymne')).toHaveCount(0);
    await expect(buzzer(max.page, 'blocked')).toBeDisabled();
    await expect(max.page.getByText(fr.modes.blindtest.found.title)).toBeVisible();
    await expect(buzzer(lea.page, 'open')).toBeEnabled();
    await expect.poll(async () => (await audioOf(display)).paused).toBe(false);
    expect((await audioOf(display)).currentTime).toBeGreaterThanOrEqual(paused - 0.1);

    // Léa has the hand: the title is not to judge anymore, and the TV still hides the artist.
    await buzzer(lea.page, 'open').tap();
    await expect(buzzer(lea.page, 'won')).toBeVisible();
    await expect(
        gm.getByRole('button', { name: fr.modes.blindtest.gm.titleFound, exact: true }),
    ).toHaveCount(0);
    await expect(display.getByText('Beethoven')).toHaveCount(0);

    // She gives the artist: nothing is left to find, and the track is revealed with the points.
    await judge(gm, fr.modes.blindtest.gm.artistFound);
    await expect(display.getByRole('heading', { name: "L'Hymne à la joie" })).toBeVisible();
    await expect(
        display.getByText(
            fill(fr.modes.blindtest.display.artist, { artist: 'Ludwig van Beethoven' }),
        ),
    ).toBeVisible();
    await expect(
        display.getByText(fill(fr.modes.blindtest.artistFoundBy, { nickname: lea.nickname })),
    ).toBeVisible();
    await expect(display.getByText(titleFound)).toBeVisible();
    const earned = (points: string) => fill(fr.modes.blindtest.player.pointsEarned, { points });
    await expect(max.page.getByText(earned('500'))).toBeVisible();
    await expect(lea.page.getByText(earned('500'))).toBeVisible();
    await expect(zoe.page.getByText(earned('0'))).toBeVisible();
    await expect(buzzer(zoe.page, 'closed')).toBeDisabled();
    await expect.poll(async () => (await audioOf(display)).paused).toBe(true);

    // The second track, revealed by the game master while the music plays: nobody scores.
    await gm.getByRole('button', { name: fr.modes.blindtest.gm.nextTrack }).click();
    await expect(display.getByRole('heading', { name: track(2) })).toBeVisible();
    await gm.getByRole('button', { name: fr.modes.blindtest.gm.play }).click();
    await expect(buzzer(zoe.page, 'open')).toBeEnabled();
    await gm.getByRole('button', { name: fr.modes.blindtest.gm.revealAnswer }).click();
    await expect(display.getByRole('heading', { name: 'Au clair de la lune' })).toBeVisible();
    await expect(display.getByText(fr.modes.blindtest.nobodyFound)).toBeVisible();
    await expect.poll(async () => (await audioOf(display)).paused).toBe(true);
    await expect(zoe.page.getByText(earned('0'))).toBeVisible();

    // The last track revealed, the game master ends the round, and with it the game.
    await gm.getByRole('button', { name: fr.modes.blindtest.gm.endRound }).click();
    await expect(display.getByText(fr.game.finished)).toBeVisible();
});

test('a TV screen reloaded while the music plays goes on where it is', async ({ table }) => {
    const { display, gm } = table;
    await unlockAudio(display);
    await startGame(gm);

    // The first track skipped: the second one is announced at 9.6 s into its file.
    await gm.getByRole('button', { name: fr.modes.blindtest.gm.skipTrack }).click();
    await gm
        .getByRole('dialog')
        .getByRole('button', { name: fr.modes.blindtest.gm.skipConfirm.confirm, exact: true })
        .click();
    await expect(display.getByRole('heading', { name: track(2) })).toBeVisible();
    await expect.poll(async () => (await audioOf(display)).currentTime).toBeCloseTo(9.6, 1);

    await gm.getByRole('button', { name: fr.modes.blindtest.gm.play }).click();
    await expect.poll(async () => (await audioOf(display)).currentTime).toBeGreaterThan(11);

    // Reloaded, it does not start the excerpt over.
    await display.reload();
    await unlockAudio(display);
    await expect.poll(async () => (await audioOf(display)).currentTime).toBeGreaterThan(11.5);
    expect((await audioOf(display)).paused).toBe(false);
});

test('an excerpt the TV screen cannot load leaves it as it is, and the console alone hears of it', async ({
    table,
}) => {
    const { display, gm } = table;
    await display.route('**/media/**', (route) => route.fulfill({ status: 500 }));
    await unlockAudio(display);
    await startGame(gm);
    await gm.getByRole('button', { name: fr.modes.blindtest.gm.play }).click();

    await expect(display.getByRole('heading', { name: track(1) })).toBeVisible();
    await expect(display.getByText(fr.modes.blindtest.display.listen)).toBeVisible();
    for (const page of [display, ...table.players.map((player) => player.page)]) {
        await expect(page.getByText(/incident|erreur|problème/i)).toHaveCount(0);
    }
    await gm.getByRole('button', { name: /incident/i }).click();
    await expect(gm.getByText(fr.gm.incidents.codes.DisplayMediaFailed)).toHaveCount(1);
});
