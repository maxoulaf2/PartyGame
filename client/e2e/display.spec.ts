import { expect, test, type Page } from '@playwright/test';
import type {
    DisplayPlayer,
    DisplayRoundView,
    DisplaySnapshot,
    GameId,
    Phase,
    PlayerId,
    RoundId,
    RoundInfo,
} from '../src/shared/contracts';
import { countText } from '../src/shared/i18n/countText.ts';
import { roundText } from '../src/shared/i18n/fill.ts';
import { fr } from '../src/shared/i18n/fr.ts';
import { serveDisplaySnapshot } from './fakeHub.ts';
import { advertisedAddress } from './gameServer.ts';
import { trackExternalRequests } from './localRequests.ts';
import { joinOnNewPhone, uniqueNickname } from './players.ts';

// The preview server listens on the port set in playwright.config.ts: the QR code must keep it.
const expectedUrl = `http://${advertisedAddress}:4173/`;

test.use({ viewport: { width: 1920, height: 1080 } });

function playerList(page: Page) {
    return page.getByRole('list', { name: fr.display.playerListLabel });
}

const fakeRound: RoundInfo = {
    roundId: '0f8fad5b-d9cb-469f-a165-70867728950e' as RoundId,
    number: 1,
    count: 3,
    title: 'Échauffement',
};

function fakeSnapshot(
    players: readonly DisplayPlayer[],
    joinAddress: string | null = advertisedAddress,
    phase: Phase = 'Lobby',
    roundView: DisplayRoundView | null = null,
): DisplaySnapshot {
    return {
        gameId: '6f9619ff-8b86-d011-b42d-00cf4fc964ff' as GameId,
        version: 1,
        phase,
        joinAddress,
        players,
        packTitle: null,
        round: phase === 'Lobby' ? null : fakeRound,
        roundView,
    };
}

function fakePlayer(index: number, nickname: string, isConnected = true): DisplayPlayer {
    const id = `${index.toString(16).padStart(8, '0')}-0000-0000-0000-000000000000` as PlayerId;
    return { id, nickname, isConnected };
}

/** Checks that `box` stays clear of the 5% a TV may crop on each side. */
function expectWithinSafeArea(
    box: { x: number; y: number; width: number; height: number } | null,
    viewport: { width: number; height: number },
): void {
    expect(box).not.toBeNull();
    if (box) {
        expect(box.x).toBeGreaterThanOrEqual(viewport.width * 0.05);
        expect(box.y).toBeGreaterThanOrEqual(viewport.height * 0.05);
        expect(box.x + box.width).toBeLessThanOrEqual(viewport.width * 0.95);
        expect(box.y + box.height).toBeLessThanOrEqual(viewport.height * 0.95);
    }
}

test('/display/ shows a QR code and the join url without external requests', async ({ page }) => {
    const external = trackExternalRequests(page);

    await page.goto('/display/');

    const qrCode = page.getByRole('img', { name: fr.display.qrCodeLabel });
    await expect(qrCode).toBeVisible();
    await expect(qrCode).toHaveAttribute('data-qr-text', expectedUrl);
    await expect(page.getByText(expectedUrl)).toBeVisible();
    await expect(page.getByText(fr.display.scanToJoin)).toBeVisible();
    expect(external).toEqual([]);
});

test('/display/ shows players as they join, then dims those who leave', async ({
    page,
    browser,
    baseURL,
}) => {
    await page.goto('/display/');
    await expect(page.getByRole('img', { name: fr.display.qrCodeLabel })).toBeVisible();

    // The last nickname would be markup if the page ever read it as HTML.
    const nicknames = [uniqueNickname('Zoé'), uniqueNickname('Max'), uniqueNickname('<b>&🎉')];
    const phones: Page[] = [];
    for (const nickname of nicknames) {
        phones.push(await joinOnNewPhone(browser, baseURL, nickname));
        await expect(playerList(page).getByText(nickname, { exact: true })).toBeVisible({
            timeout: 1000,
        });
    }
    await expect(playerList(page).locator('b')).toHaveCount(0);

    // Arrival order is kept: each one comes after those who joined before.
    const shown = await playerList(page).locator('.nickname').allInnerTexts();
    const positions = nicknames.map((nickname) => shown.indexOf(nickname));
    expect(positions[0]).toBeGreaterThanOrEqual(0);
    expect(positions).toEqual([...positions].sort((a, b) => a - b));

    await phones[0]?.context().close();

    const left = playerList(page).locator('li', { hasText: nicknames[0] });
    await expect(left).toContainText(fr.display.disconnected);
    await expect(left.locator('svg')).toBeVisible();
    expect(Number(await left.evaluate((item) => getComputedStyle(item).opacity))).toBeLessThan(1);

    await Promise.all(phones.slice(1).map((phone) => phone.context().close()));
});

// Outside a round, the lobby stays on the TV with what is going on, for late arrivals to join.
const notices = {
    Lobby: [],
    BetweenRounds: [roundText(fr.game.roundEnded, fakeRound), fr.display.betweenRounds],
    Finished: [fr.game.finished, fr.display.finished],
} as const;

for (const phase of ['Lobby', 'BetweenRounds', 'Finished'] as const) {
    test(`/display/ fits 20 long nicknames on a 1080p screen in phase ${phase}, readable and clear of the edges`, async ({
        page,
    }) => {
        const players = Array.from({ length: 20 }, (_, index) =>
            fakePlayer(
                index + 1,
                `Joueur n°${String(index + 1).padStart(2, '0')} WMWM`,
                index !== 3,
            ),
        );
        expect(players.every((player) => [...player.nickname].length === 16)).toBe(true);
        await serveDisplaySnapshot(page, fakeSnapshot(players, advertisedAddress, phase));

        await page.goto('/display/');

        const viewport = page.viewportSize();
        if (!viewport) {
            throw new Error('The test needs a fixed viewport');
        }
        await expect(playerList(page).locator('li')).toHaveCount(20);
        await expect(page.getByText(countText(fr.display.playersJoined, 20))).toBeVisible();
        for (const player of players) {
            const nickname = playerList(page).getByText(player.nickname, { exact: true });
            await expect(nickname).toBeVisible();
            expectWithinSafeArea(await nickname.boundingBox(), viewport);
            // About 3 cm high on a 55" TV: readable from 3 m.
            const fontSize = await nickname.evaluate((element) =>
                parseFloat(getComputedStyle(element).fontSize),
            );
            expect(fontSize).toBeGreaterThanOrEqual(30);
        }
        for (const element of [
            page.getByRole('img', { name: fr.display.qrCodeLabel }),
            page.getByText(expectedUrl),
            page.getByRole('heading', { name: fr.app.name }),
            ...notices[phase].map((notice) => page.getByText(notice, { exact: true })),
        ]) {
            expectWithinSafeArea(await element.boundingBox(), viewport);
        }
        const overflows = await page.evaluate(() => {
            const root = document.documentElement;
            return root.scrollHeight > root.clientHeight || root.scrollWidth > root.clientWidth;
        });
        expect(overflows).toBe(false);
    });
}

test('/display/ keeps the player list when the server knows no address', async ({ page }) => {
    await serveDisplaySnapshot(page, fakeSnapshot([fakePlayer(1, 'Zoé')], null));

    await page.goto('/display/');

    await expect(page.getByText(fr.display.joinUnavailable)).toBeVisible();
    await expect(page.getByRole('img', { name: fr.display.qrCodeLabel })).toHaveCount(0);
    await expect(playerList(page).getByText('Zoé', { exact: true })).toBeVisible();
});

test('/display/ shows the round in progress with the view of its mode', async ({ page }) => {
    await serveDisplaySnapshot(
        page,
        fakeSnapshot([fakePlayer(1, 'Zoé')], advertisedAddress, 'Round', { type: 'quiz' }),
    );

    await page.goto('/display/');

    await expect(page.getByRole('heading', { name: fakeRound.title })).toBeVisible();
    await expect(page.getByText(roundText(fr.game.round, fakeRound))).toBeVisible();
    await expect(page.getByRole('img', { name: fr.display.qrCodeLabel })).toHaveCount(0);
});

test('/display/ waits neutrally on a round of a mode it does not know', async ({ page }) => {
    const unknownView = { type: 'blindtest' } as unknown as DisplayRoundView;
    await serveDisplaySnapshot(
        page,
        fakeSnapshot([fakePlayer(1, 'Zoé')], advertisedAddress, 'Round', unknownView),
    );

    await page.goto('/display/');

    await expect(page.getByText(fr.display.inProgress, { exact: true })).toBeVisible();
    // Never an empty screen: the lobby stays, with its QR code and its players.
    await expect(page.getByRole('img', { name: fr.display.qrCodeLabel })).toBeVisible();
    await expect(playerList(page).getByText('Zoé', { exact: true })).toBeVisible();
    await expect(page.getByText('blindtest')).toHaveCount(0);
});
