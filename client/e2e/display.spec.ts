import { expect, test, type Browser, type BrowserType, type Page } from '@playwright/test';
import type {
    QuizChoiceLetter,
    DisplayPlayer,
    DisplayRoundView,
    DisplaySnapshot,
    GameId,
    OpenQuestionDisplayView,
    Phase,
    PlayerId,
    QuizDisplayView,
    RankedPlayer,
    RoundId,
    RoundInfo,
} from '../src/shared/contracts';
import { countText } from '../src/shared/i18n/countText.ts';
import { fill, roundText } from '../src/shared/i18n/fill.ts';
import { fr } from '../src/shared/i18n/fr.ts';
import { rankText } from '../src/shared/i18n/rankText.ts';
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
    mode: 'quiz',
    description: null,
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
        ranking: [],
        joinCodeShown: false,
        preview: null,
    };
}

function fakePlayer(index: number, nickname: string, isConnected = true): DisplayPlayer {
    const id = `${index.toString(16).padStart(8, '0')}-0000-0000-0000-000000000000` as PlayerId;
    return { id, nickname, isConnected };
}

/** `count` players with nicknames of 16 wide characters, the longest allowed, the fourth one disconnected. */
function longNicknames(count: number): DisplayPlayer[] {
    const players = Array.from({ length: count }, (_, index) =>
        fakePlayer(index + 1, `Joueur n°${String(index + 1).padStart(2, '0')} WMWM`, index !== 3),
    );
    expect(players.every((player) => [...player.nickname].length === 16)).toBe(true);
    return players;
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

test('/display/ fits 20 long nicknames on a 1080p screen in the lobby, readable and clear of the edges', async ({
    page,
}) => {
    const players = longNicknames(20);
    await serveDisplaySnapshot(page, fakeSnapshot(players));

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
    ]) {
        expectWithinSafeArea(await element.boundingBox(), viewport);
    }
    const overflows = await page.evaluate(() => {
        const root = document.documentElement;
        return root.scrollHeight > root.clientHeight || root.scrollWidth > root.clientWidth;
    });
    expect(overflows).toBe(false);
});

/** The ranking of `players` as the server sends it: by score, ties sharing their rank. */
function fakeRanking(players: readonly DisplayPlayer[], scores: readonly number[]): RankedPlayer[] {
    return players.map((player, index) => {
        const score = scores[index] ?? 0;
        const rank = scores.filter((other) => other > score).length + 1;
        const isTied = scores.filter((other) => other === score).length > 1;
        return { ...player, rank, isTied, score };
    });
}

function rankingList(page: Page) {
    return page.getByRole('list', { name: fr.game.rankingLabel });
}

test('/display/ ranks the players between two rounds as the server sends them', async ({
    page,
}) => {
    const players = [
        fakePlayer(1, 'Max'),
        fakePlayer(2, 'Zoé'),
        fakePlayer(3, 'Léa', false),
        fakePlayer(4, '<b>&🎉'),
    ];
    const ranking = fakeRanking(players, [2350, 1000, 1000, 0]);
    await serveDisplaySnapshot(page, {
        ...fakeSnapshot(players, advertisedAddress, 'BetweenRounds'),
        ranking,
    });

    await page.goto('/display/');

    await expect(
        page.getByRole('heading', { name: fill(fr.game.rankingAfter, { number: 1 }) }),
    ).toBeVisible();
    await expect(page.getByText(roundText(fr.game.roundEnded, fakeRound))).toBeVisible();
    const points = (count: number) => countText(fr.game.points, count);
    await expect(rankingList(page).getByRole('listitem')).toHaveText([
        `${rankText(fr.game.rank, 1)} Max ${points(2350)}`,
        `${rankText(fr.game.rank, 2)} Zoé ${points(1000)}`,
        `${rankText(fr.game.rank, 2)} Léa (${fr.display.disconnected}) ${points(1000)}`,
        `${rankText(fr.game.rank, 4)} <b>&🎉 ${points(0)}`,
    ]);
    await expect(rankingList(page).locator('b')).toHaveCount(0);
    // Dimmed and marked with an icon, never by colour alone.
    const left = rankingList(page).getByRole('listitem').nth(2);
    await expect(left.locator('svg')).toBeVisible();
    // The lobby gives way to the ranking, but late arrivals can still join.
    await expect(playerList(page)).toHaveCount(0);
    await expect(page.getByRole('img', { name: fr.display.qrCodeLabel })).toBeVisible();
});

test('/display/ fits a ranking of 20 long nicknames on a 1080p screen, readable and clear of the edges', async ({
    page,
}) => {
    const players = longNicknames(20);
    const ranking = fakeRanking(
        players,
        players.map((_, index) => 12_350 - 650 * Math.floor(index / 2)),
    );
    await serveDisplaySnapshot(page, {
        ...fakeSnapshot(players, advertisedAddress, 'BetweenRounds'),
        ranking,
    });

    await page.goto('/display/');

    const viewport = page.viewportSize();
    if (!viewport) {
        throw new Error('The test needs a fixed viewport');
    }
    const items = rankingList(page).getByRole('listitem');
    await expect(items).toHaveCount(20);
    for (const item of await items.all()) {
        await expect(item).toBeVisible();
        expectWithinSafeArea(await item.boundingBox(), viewport);
        // About 3 cm high on a 55" TV: readable from 3 m.
        const fontSize = await item.evaluate((element) =>
            parseFloat(getComputedStyle(element).fontSize),
        );
        expect(fontSize).toBeGreaterThanOrEqual(30);
    }
    // Ranks read top to bottom, column by column.
    const first = await items.nth(0).boundingBox();
    const second = await items.nth(1).boundingBox();
    expect(second?.y).toBeGreaterThan(first?.y ?? Infinity);
    for (const element of [
        page.getByRole('heading', { name: fill(fr.game.rankingAfter, { number: 1 }) }),
        page.getByRole('img', { name: fr.display.qrCodeLabel }),
    ]) {
        expectWithinSafeArea(await element.boundingBox(), viewport);
    }
    const overflows = await page.evaluate(() => {
        const root = document.documentElement;
        return root.scrollHeight > root.clientHeight || root.scrollWidth > root.clientWidth;
    });
    expect(overflows).toBe(false);
});

function podium(page: Page) {
    return page.getByRole('list', { name: fr.game.podiumLabel });
}

/** The players the podium shows on the step of `rank`. */
function podiumStep(page: Page, rank: number) {
    return podium(page).getByRole('list', {
        name: fill(fr.game.podiumStepLabel, { rank: rankText(fr.game.rank, rank) }),
    });
}

/** The block of the step of `rank`, which shows its rank and its score. */
function podiumBlock(page: Page, rank: number) {
    return podium(page).locator(`[data-rank="${rank}"]`);
}

test('/display/ shows the final ranking as the server sends it: a podium, then the others', async ({
    page,
}) => {
    const players = [
        fakePlayer(1, 'Max'),
        fakePlayer(2, 'Léa', false),
        fakePlayer(3, 'Zoé'),
        fakePlayer(4, '<b>&🎉'),
        fakePlayer(5, 'Tom'),
    ];
    const ranking = fakeRanking(players, [3000, 2000, 2000, 1000, 0]);
    await serveDisplaySnapshot(page, {
        ...fakeSnapshot(players, advertisedAddress, 'Finished'),
        ranking,
    });

    await page.goto('/display/');

    await expect(page.getByRole('heading', { name: fr.game.finalRanking })).toBeVisible();
    await expect(page.getByText(fr.game.finished, { exact: true })).toBeVisible();
    const points = (count: number) => countText(fr.game.points, count);
    // Ex aequo share their step, and the rank after them is skipped: no third step.
    await expect(podiumStep(page, 1).getByRole('listitem')).toHaveText(['Max']);
    await expect(podiumStep(page, 2).getByRole('listitem')).toHaveText([
        `Léa (${fr.display.disconnected})`,
        'Zoé',
    ]);
    await expect(podiumStep(page, 3)).toHaveCount(0);
    await expect(podiumBlock(page, 1)).toHaveText(`${rankText(fr.game.rank, 1)} ${points(3000)}`);
    await expect(podiumBlock(page, 2)).toHaveText(`${rankText(fr.game.rank, 2)} ${points(2000)}`);
    await expect(
        page.getByRole('list', { name: fr.game.restLabel }).getByRole('listitem'),
    ).toHaveText([
        `${rankText(fr.game.rank, 4)} <b>&🎉 ${points(1000)}`,
        `${rankText(fr.game.rank, 5)} Tom ${points(0)}`,
    ]);
    await expect(page.locator('main b')).toHaveCount(0);
    // A disconnected player is dimmed and marked with an icon, never by colour alone.
    await expect(podiumStep(page, 2).getByRole('listitem').first().locator('svg')).toBeVisible();

    // A podium: the first step in the middle and higher, the second on its left.
    const first = await podiumBlock(page, 1).boundingBox();
    const second = await podiumBlock(page, 2).boundingBox();
    expect(second?.x).toBeLessThan(first?.x ?? -Infinity);
    expect(first?.height).toBeGreaterThan(second?.height ?? Infinity);
    expect(first?.y).toBeLessThan(second?.y ?? -Infinity);

    // The game is over: neither the lobby nor its QR code anymore.
    await expect(playerList(page)).toHaveCount(0);
    await expect(page.getByRole('img', { name: fr.display.qrCodeLabel })).toHaveCount(0);
});

test('/display/ announces a round with its rule and a description of 300 characters, readable and clear of the edges', async ({
    page,
}) => {
    const description = 'Une présentation bien longue de la manche, '.repeat(7).slice(0, 300);
    const round: RoundInfo = {
        ...fakeRound,
        title: 'Le grand final du blind test',
        mode: 'blindtest',
        description,
    };
    await serveDisplaySnapshot(page, {
        ...fakeSnapshot([], advertisedAddress, 'RoundIntro'),
        round,
    });

    await page.goto('/display/');

    const viewport = page.viewportSize();
    if (!viewport) {
        throw new Error('The test needs a fixed viewport');
    }
    expect(description).toHaveLength(300);
    const shown = [
        page.getByText(roundText(fr.game.round, round)),
        page.getByRole('heading', { name: round.title }),
        page.getByText(fr.modes.blindtest.name, { exact: true }),
        page.getByText(fr.modes.blindtest.rule),
        page.getByText(description),
    ];
    for (const element of shown) {
        await expect(element).toBeVisible();
        expectWithinSafeArea(await element.boundingBox(), viewport);
        // About 2 cm high on a 55" TV at the least: readable from 3 m.
        const fontSize = await element.evaluate((e) => parseFloat(getComputedStyle(e).fontSize));
        expect(fontSize).toBeGreaterThanOrEqual(36);
    }
    const overflows = await page.evaluate(() => {
        const main = document.querySelector('main');
        return (
            !main || main.scrollHeight > main.clientHeight || main.scrollWidth > main.clientWidth
        );
    });
    expect(overflows).toBe(false);
});

test('/display/ announces a round without description with its rule alone', async ({ page }) => {
    await serveDisplaySnapshot(page, fakeSnapshot([], advertisedAddress, 'RoundIntro'));

    await page.goto('/display/');

    await expect(page.getByText(fr.modes.quiz.rule)).toBeVisible();
    await expect(page.locator('main p')).toHaveCount(3);
});

// Scores of 20 players: distinct but for a few ex aequo, every player ex aequo, and one player
// ahead of 19 ex aequo.
const finalScores = {
    'mostly distinct scores': Array.from(
        { length: 20 },
        (_, index) => 12_350 - 650 * Math.floor(index / 2),
    ),
    'every player ex aequo': Array.from({ length: 20 }, () => 0),
    'one player ahead of 19 ex aequo': Array.from({ length: 20 }, (_, index) =>
        index === 0 ? 1000 : 0,
    ),
    '18 ex aequo on the third step': Array.from({ length: 20 }, (_, index) =>
        index === 0 ? 2000 : index === 1 ? 1000 : 0,
    ),
};

for (const [scenario, scores] of Object.entries(finalScores)) {
    test(`/display/ fits a final ranking of 20 long nicknames on a 1080p screen with ${scenario}, readable and clear of the edges`, async ({
        page,
    }) => {
        const players = longNicknames(20);
        await serveDisplaySnapshot(page, {
            ...fakeSnapshot(players, advertisedAddress, 'Finished'),
            ranking: fakeRanking(players, scores),
        });

        await page.goto('/display/');

        const viewport = page.viewportSize();
        if (!viewport) {
            throw new Error('The test needs a fixed viewport');
        }
        const onPodium = podium(page).getByRole('list').getByRole('listitem');
        const rest = page.getByRole('list', { name: fr.game.restLabel }).getByRole('listitem');
        await expect(page.getByRole('heading', { name: fr.game.finalRanking })).toBeVisible();
        expect((await onPodium.count()) + (await rest.count())).toBe(20);
        for (const player of players) {
            const nickname = page.locator('main').getByText(player.nickname, { exact: true });
            await expect(nickname).toBeVisible();
            expectWithinSafeArea(await nickname.boundingBox(), viewport);
            // About 3 cm high on a 55" TV: readable from 3 m.
            const fontSize = await nickname.evaluate((element) =>
                parseFloat(getComputedStyle(element).fontSize),
            );
            expect(fontSize).toBeGreaterThanOrEqual(30);
        }
        // On the podium, each nickname stays whole on a single line.
        for (const item of await onPodium.all()) {
            const whole = await item.evaluate(
                (element) =>
                    element.getClientRects().length === 1 &&
                    element.scrollWidth <= element.clientWidth,
            );
            expect(whole).toBe(true);
        }
        for (const element of [
            page.getByRole('heading', { name: fr.game.finalRanking }),
            ...(await podium(page).locator('[data-rank]').all()),
            ...(await rest.all()),
        ]) {
            expectWithinSafeArea(await element.boundingBox(), viewport);
        }
        // Nothing scrolls, and nothing is cut by the screen either.
        const overflows = await page.evaluate(() => {
            const root = document.documentElement;
            const main = document.querySelector('main');
            return (
                root.scrollHeight > root.clientHeight ||
                root.scrollWidth > root.clientWidth ||
                !main ||
                main.scrollHeight > main.clientHeight ||
                main.scrollWidth > main.clientWidth
            );
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

/** A question in presentation, every choice shown, as the quiz shows it on the TV screen. */
function quizView(view: Partial<QuizDisplayView> = {}): QuizDisplayView {
    return {
        type: 'quiz',
        questionNumber: 3,
        questionCount: 5,
        phase: 'Presentation',
        text: 'Quelle est la capitale de l’Australie ?',
        imageUrl: null,
        choices: [
            { letter: 'A', text: 'Sydney' },
            { letter: 'B', text: 'Canberra' },
            { letter: 'C', text: 'Melbourne' },
            { letter: 'D', text: 'Perth' },
        ],
        choiceCount: 4,
        answersCloseAt: null,
        answeredCount: 0,
        participantCount: 0,
        reveal: null,
        ...view,
    };
}

/** A text of exactly `length` characters, made of words of usual lengths. */
function longText(start: string, length: number): string {
    return (
        `${start} ${'avec des mots de longueur habituelle '.repeat(10)}`.slice(0, length - 1) + '?'
    );
}

/** Serves an image of 1600 × 1200 at `url`, larger than the room the TV screen gives it. */
async function serveImage(page: Page, url: string): Promise<void> {
    const svg =
        '<svg xmlns="http://www.w3.org/2000/svg" width="1600" height="1200"><rect width="1600" height="1200" fill="#4ba3ff"/></svg>';
    await page.route(`**${url}`, (route) =>
        route.fulfill({ status: 200, contentType: 'image/svg+xml', body: svg }),
    );
}

test('/display/ presents the question of the round in progress, with its choices', async ({
    page,
}) => {
    await serveDisplaySnapshot(
        page,
        fakeSnapshot([fakePlayer(1, 'Zoé')], advertisedAddress, 'Round', quizView()),
    );

    await page.goto('/display/');

    await expect(page.getByText(fakeRound.title, { exact: true })).toBeVisible();
    await expect(
        page.getByText(fill(fr.modes.quiz.question, { number: 3, count: 5 })),
    ).toBeVisible();
    await expect(
        page.getByRole('heading', { name: 'Quelle est la capitale de l’Australie ?' }),
    ).toBeVisible();
    const choices = page
        .getByRole('list', { name: fr.modes.quiz.choicesLabel })
        .getByRole('listitem');
    await expect(choices).toHaveText(['A Sydney', 'B Canberra', 'C Melbourne', 'D Perth']);
    // Each letter has a shape of its own, so that no choice is told apart by its color alone.
    const shapes = await choices.evaluateAll((items) =>
        items.map((item) => getComputedStyle(item.querySelector('.shape') as Element).clipPath),
    );
    expect(new Set(shapes).size).toBe(4);
    await expect(page.getByRole('img', { name: fr.modes.quiz.display.imageLabel })).toHaveCount(0);
    await expect(page.getByRole('img', { name: fr.display.qrCodeLabel })).toHaveCount(0);
});

test('/display/ shows the QR code in the top left corner over the round when the game master asks', async ({
    page,
}) => {
    await serveDisplaySnapshot(page, {
        ...fakeSnapshot([fakePlayer(1, 'Zoé')], advertisedAddress, 'Round', quizView()),
        joinCodeShown: true,
    });

    await page.goto('/display/');

    const qrCode = page.getByRole('img', { name: fr.display.qrCodeLabel });
    await expect(qrCode).toHaveAttribute('data-qr-text', expectedUrl);
    // Clear of the 5% a TV may crop, in the top left quarter.
    const box = await qrCode.boundingBox();
    expect(box).not.toBeNull();
    expect(box!.x).toBeGreaterThanOrEqual(1920 * 0.05);
    expect(box!.y).toBeGreaterThanOrEqual(1080 * 0.05);
    expect(box!.x + box!.width).toBeLessThan(1920 / 2);
    expect(box!.y + box!.height).toBeLessThan(1080 / 2);
    // The round goes on underneath.
    await expect(
        page.getByRole('heading', { name: 'Quelle est la capitale de l’Australie ?' }),
    ).toBeVisible();
});

test('/display/ shows the number of the question alone until the game master shows it', async ({
    page,
}) => {
    // Even its image stays hidden: the snapshot holds nothing of the question yet.
    await serveDisplaySnapshot(
        page,
        fakeSnapshot(
            [fakePlayer(1, 'Zoé')],
            advertisedAddress,
            'Round',
            quizView({ text: null, choices: [] }),
        ),
    );

    await page.goto('/display/');

    await expect(page.getByText(fakeRound.title, { exact: true })).toBeVisible();
    await expect(
        page.getByText(fill(fr.modes.quiz.display.upcoming, { number: 3 }), { exact: true }),
    ).toBeVisible();
    await expect(page.getByRole('heading')).toHaveCount(0);
    await expect(page.getByRole('img', { name: fr.modes.quiz.display.imageLabel })).toHaveCount(0);
    await expect(
        page.getByRole('list', { name: fr.modes.quiz.choicesLabel }).getByRole('listitem'),
    ).toHaveCount(0);
});

test('/display/ shows the choices one by one, each in the room it keeps once all are shown', async ({
    page,
}) => {
    // Without the fade, so that every box is measured where it stays.
    const boxesOf = async (target: Page, shownCount: number) => {
        await target.emulateMedia({ reducedMotion: 'reduce' });
        const view = quizView();
        await serveDisplaySnapshot(
            target,
            fakeSnapshot(
                [fakePlayer(1, 'Zoé')],
                advertisedAddress,
                'Round',
                quizView({ choices: view.choices.slice(0, shownCount) }),
            ),
        );
        await target.goto('/display/');
        const choices = target
            .getByRole('list', { name: fr.modes.quiz.choicesLabel })
            .getByRole('listitem');
        await expect(choices).toHaveText(
            ['A Sydney', 'B Canberra', 'C Melbourne', 'D Perth'].slice(0, shownCount),
        );
        const heading = target.getByRole('heading', { name: 'Quelle est la capitale' });
        return {
            heading: await heading.boundingBox(),
            choices: await Promise.all((await choices.all()).map((choice) => choice.boundingBox())),
        };
    };

    const partly = await boxesOf(page, 2);
    const full = await boxesOf(await page.context().newPage(), 4);

    // Nothing moves as the other choices show.
    expect(partly.heading).toEqual(full.heading);
    expect(partly.choices).toEqual(full.choices.slice(0, 2));
});

test('/display/ fits a long illustrated question and four long choices on a 1080p screen, readable and clear of the edges', async ({
    page,
}) => {
    const imageUrl = '/media/illustration-de-test';
    await serveImage(page, imageUrl);
    const text = longText('Laquelle de ces propositions est la bonne', 200);
    const view = quizView({
        text,
        imageUrl,
        // Answering, so that the countdown and the count of the answers are on screen as well.
        phase: 'Answering',
        answersCloseAt: Date.now() + 60_000,
        answeredCount: 19,
        participantCount: 20,
        choices: (['A', 'B', 'C', 'D'] as const).map((letter) => ({
            letter,
            text: longText(`Proposition ${letter}`, 80),
        })),
    });
    expect([...text].length).toBe(200);
    expect(view.choices.every((choice) => [...choice.text].length === 80)).toBe(true);
    await serveDisplaySnapshot(
        page,
        fakeSnapshot([fakePlayer(1, 'Zoé')], advertisedAddress, 'Round', view),
    );

    await page.goto('/display/');

    const viewport = page.viewportSize();
    if (!viewport) {
        throw new Error('The test needs a fixed viewport');
    }
    const image = page.getByRole('img', { name: fr.modes.quiz.display.imageLabel });
    await expect(image).toBeVisible();
    await expect.poll(() => image.evaluate((img: HTMLImageElement) => img.complete)).toBe(true);
    const question = page.getByRole('heading', { name: text });
    const choices = page
        .getByRole('list', { name: fr.modes.quiz.choicesLabel })
        .getByRole('listitem');
    await expect(choices).toHaveCount(4);
    for (const element of [
        page.getByText(fakeRound.title, { exact: true }),
        page.getByText(fill(fr.modes.quiz.question, { number: 3, count: 5 })),
        image,
        page.getByRole('timer'),
        page.getByText(fill(fr.modes.quiz.answered, { answered: 19, participants: 20 })),
        question,
        ...(await choices.all()),
    ]) {
        await expect(element).toBeVisible();
        expectWithinSafeArea(await element.boundingBox(), viewport);
    }
    // About 3 cm high on a 55" TV: readable from 3 m.
    for (const text of [question, ...(await choices.locator('.text').all())]) {
        const fontSize = await text.evaluate((element) =>
            parseFloat(getComputedStyle(element).fontSize),
        );
        expect(fontSize).toBeGreaterThanOrEqual(30);
    }
    // Nothing scrolls, and nothing is cut by the screen of the quiz either.
    const overflows = await page.evaluate(() => {
        const root = document.documentElement;
        const main = document.querySelector('main');
        return (
            root.scrollHeight > root.clientHeight ||
            root.scrollWidth > root.clientWidth ||
            !main ||
            main.scrollHeight > main.clientHeight ||
            main.scrollWidth > main.clientWidth
        );
    });
    expect(overflows).toBe(false);
});

test('/display/ keeps the question on screen without its image when the image fails', async ({
    page,
}) => {
    const imageUrl = '/media/introuvable';
    await page.route(`**${imageUrl}`, (route) => route.fulfill({ status: 404 }));
    await serveDisplaySnapshot(
        page,
        fakeSnapshot([fakePlayer(1, 'Zoé')], advertisedAddress, 'Round', quizView({ imageUrl })),
    );

    await page.goto('/display/');

    await expect(
        page.getByRole('heading', { name: 'Quelle est la capitale de l’Australie ?' }),
    ).toBeVisible();
    await expect(page.getByRole('img', { name: fr.modes.quiz.display.imageLabel })).toHaveCount(0);
    await expect(
        page.getByRole('list', { name: fr.modes.quiz.choicesLabel }).getByRole('listitem'),
    ).toHaveCount(4);
});

test('/display/ waits neutrally on a round of a mode it does not know', async ({ page }) => {
    const unknownView = { type: 'karaoke' } as unknown as DisplayRoundView;
    await serveDisplaySnapshot(
        page,
        fakeSnapshot([fakePlayer(1, 'Zoé')], advertisedAddress, 'Round', unknownView),
    );

    await page.goto('/display/');

    await expect(page.getByText(fr.display.inProgress, { exact: true })).toBeVisible();
    // Never an empty screen: the lobby stays, with its QR code and its players.
    await expect(page.getByRole('img', { name: fr.display.qrCodeLabel })).toBeVisible();
    await expect(playerList(page).getByText('Zoé', { exact: true })).toBeVisible();
    await expect(page.getByText('karaoke')).toHaveCount(0);
});

/** The seconds a countdown of the page shows. */
async function shownSeconds(page: Page): Promise<number> {
    return Number(await page.getByRole('timer').locator('.seconds').textContent());
}

test('/display/ counts down the answers and shows how many players answered, never what', async ({
    page,
}) => {
    // The fake hub answers no clock synchronization: the page takes its clock for the server's.
    const view = quizView({
        phase: 'Answering',
        answersCloseAt: Date.now() + 15_500,
        answeredCount: 7,
        participantCount: 9,
    });
    await serveDisplaySnapshot(
        page,
        fakeSnapshot([fakePlayer(1, 'Zoé')], advertisedAddress, 'Round', view),
    );

    await page.goto('/display/');

    await expect(
        page.getByText(fill(fr.modes.quiz.answered, { answered: 7, participants: 9 })),
    ).toBeVisible();
    const timer = page.getByRole('timer');
    await expect(timer).toBeVisible();
    await expect(timer).toContainText(fr.modes.quiz.timeLeft);
    const first = await shownSeconds(page);
    expect(first).toBeGreaterThanOrEqual(13);
    expect(first).toBeLessThanOrEqual(16);
    await expect.poll(() => shownSeconds(page), { timeout: 3_000 }).toBe(first - 1);
    await expect(page.getByText(fr.modes.quiz.timeUp)).toHaveCount(0);
    // The question and its choices stay on screen while the players answer.
    await expect(
        page.getByRole('list', { name: fr.modes.quiz.choicesLabel }).getByRole('listitem'),
    ).toHaveText(['A Sydney', 'B Canberra', 'C Melbourne', 'D Perth']);
});

test('/display/ counts the answers given while the choices show, without countdown', async ({
    page,
}) => {
    const view = quizView({
        choices: quizView().choices.slice(0, 2),
        answeredCount: 3,
        participantCount: 9,
    });
    await serveDisplaySnapshot(
        page,
        fakeSnapshot([fakePlayer(1, 'Zoé')], advertisedAddress, 'Round', view),
    );

    await page.goto('/display/');

    await expect(
        page.getByText(fill(fr.modes.quiz.answered, { answered: 3, participants: 9 })),
    ).toBeVisible();
    await expect(page.getByRole('timer')).toHaveCount(0);
    await expect(page.getByText(fr.modes.quiz.timeUp)).toHaveCount(0);
});

test('/display/ stops the countdown at 0 and waits for the server to lock the answers', async ({
    page,
}) => {
    const view = quizView({
        phase: 'Answering',
        answersCloseAt: Date.now() + 1_200,
        answeredCount: 1,
        participantCount: 2,
    });
    await serveDisplaySnapshot(
        page,
        fakeSnapshot([fakePlayer(1, 'Zoé')], advertisedAddress, 'Round', view),
    );

    await page.goto('/display/');

    await expect.poll(() => shownSeconds(page), { timeout: 4_000 }).toBe(0);
    // The countdown decides nothing: only a snapshot ends the answers.
    await expect(page.getByText(fr.modes.quiz.timeUp)).toHaveCount(0);
});

test('/display/ shows that the time is up once the countdown locked the answers', async ({
    page,
}) => {
    const view = quizView({ phase: 'Locked', answeredCount: 2, participantCount: 3 });
    await serveDisplaySnapshot(
        page,
        fakeSnapshot([fakePlayer(1, 'Zoé')], advertisedAddress, 'Round', view),
    );

    await page.goto('/display/');

    await expect(page.getByText(fr.modes.quiz.timeUp)).toBeVisible();
    await expect(page.getByText(fr.modes.quiz.allAnswered)).toHaveCount(0);
    await expect(
        page.getByText(fill(fr.modes.quiz.answered, { answered: 2, participants: 3 })),
    ).toBeVisible();
    await expect(page.getByRole('timer')).toHaveCount(0);
});

test('/display/ shows that everybody answered once their last answer locked the answers', async ({
    page,
}) => {
    const view = quizView({ phase: 'Locked', answeredCount: 3, participantCount: 3 });
    await serveDisplaySnapshot(
        page,
        fakeSnapshot([fakePlayer(1, 'Zoé')], advertisedAddress, 'Round', view),
    );

    await page.goto('/display/');

    await expect(page.getByText(fr.modes.quiz.allAnswered)).toBeVisible();
    await expect(page.getByText(fr.modes.quiz.timeUp)).toHaveCount(0);
    await expect(page.getByRole('timer')).toHaveCount(0);
});

/** The question revealed, B being its correct choice, the players given having answered in order of arrival. */
function revealedView(
    answers: readonly { nickname: string; choice: QuizChoiceLetter | null }[],
    view: Partial<QuizDisplayView> = {},
): QuizDisplayView {
    return quizView({
        phase: 'Revealed',
        answeredCount: answers.filter((answer) => answer.choice !== null).length,
        participantCount: answers.length,
        reveal: {
            correctChoice: 'B',
            answers: answers.map((answer, index) => ({
                playerId: fakePlayer(index + 1, answer.nickname).id,
                ...answer,
            })),
        },
        ...view,
    });
}

function choicePlayers(page: Page, letter: QuizChoiceLetter) {
    return page.getByRole('list', {
        name: fill(fr.modes.quiz.display.choicePlayersLabel, { letter }),
    });
}

test('/display/ reveals the correct answer, who chose each choice, and who did not answer', async ({
    page,
}) => {
    const view = revealedView([
        { nickname: 'Zoé', choice: 'B' },
        { nickname: 'Max', choice: 'A' },
        { nickname: 'Léa', choice: null },
        { nickname: 'Noé', choice: 'B' },
    ]);
    await serveDisplaySnapshot(
        page,
        fakeSnapshot([fakePlayer(1, 'Zoé')], advertisedAddress, 'Round', view),
    );

    await page.goto('/display/');

    // The correct choice is told by an icon and a label, the others are dimmed.
    const choices = page.getByRole('list', { name: fr.modes.quiz.choicesLabel });
    const correct = choices.locator(':scope > li', { hasText: 'Canberra' });
    await expect(correct.getByText(fr.modes.quiz.correct)).toBeVisible();
    await expect(page.getByText(fr.modes.quiz.correct)).toHaveCount(1);
    const opacities = await choices
        .locator(':scope > li .choice')
        .evaluateAll((items) => items.map((item) => Number(getComputedStyle(item).opacity)));
    expect(opacities).toEqual([0.45, 1, 0.45, 0.45]);
    // Under each choice, how many chose it and who, in order of arrival.
    await expect(choicePlayers(page, 'B').getByRole('listitem')).toHaveText(['Zoé', 'Noé']);
    await expect(choicePlayers(page, 'A').getByRole('listitem')).toHaveText(['Max']);
    await expect(choicePlayers(page, 'C')).toHaveCount(0);
    await expect(correct.getByText(countText(fr.modes.quiz.choiceAnswers, 2))).toBeVisible();
    await expect(
        choices
            .locator(':scope > li', { hasText: 'Perth' })
            .getByText(countText(fr.modes.quiz.choiceAnswers, 0)),
    ).toBeVisible();
    await expect(
        page.getByRole('list', { name: fr.modes.quiz.display.unanswered }).getByRole('listitem'),
    ).toHaveText(['Léa']);
    await expect(page.getByText(fr.modes.quiz.timeUp)).toHaveCount(0);
});

test('/display/ fits 20 long nicknames under a single choice on a 1080p screen, readable and clear of the edges', async ({
    page,
}) => {
    const nicknames = Array.from(
        { length: 20 },
        (_, index) => `Joueur n°${String(index + 1).padStart(2, '0')} WMWM`,
    );
    expect(nicknames.every((nickname) => [...nickname].length === 16)).toBe(true);
    const imageUrl = '/media/illustration-de-test';
    await serveImage(page, imageUrl);
    const text = longText('Laquelle de ces propositions est la bonne', 200);
    const view = revealedView(
        nicknames.map((nickname) => ({ nickname, choice: 'B' })),
        {
            text,
            imageUrl,
            choices: (['A', 'B', 'C', 'D'] as const).map((letter) => ({
                letter,
                text: longText(`Proposition ${letter}`, 80),
            })),
        },
    );
    await serveDisplaySnapshot(
        page,
        fakeSnapshot([fakePlayer(1, 'Zoé')], advertisedAddress, 'Round', view),
    );

    await page.goto('/display/');

    const viewport = page.viewportSize();
    if (!viewport) {
        throw new Error('The test needs a fixed viewport');
    }
    const shown = choicePlayers(page, 'B').getByRole('listitem');
    await expect(shown).toHaveText(nicknames);
    const choices = page
        .getByRole('list', { name: fr.modes.quiz.choicesLabel })
        .locator(':scope > li');
    for (const element of [
        page.getByRole('heading', { name: text }),
        ...(await choices.all()),
        ...(await shown.all()),
    ]) {
        await expect(element).toBeVisible();
        expectWithinSafeArea(await element.boundingBox(), viewport);
    }
    // About 3 cm high on a 55" TV: readable from 3 m.
    for (const text of [...(await shown.all()), ...(await choices.locator('.text').all())]) {
        const fontSize = await text.evaluate((element) =>
            parseFloat(getComputedStyle(element).fontSize),
        );
        expect(fontSize).toBeGreaterThanOrEqual(30);
    }
    // Each nickname stays whole on a single line.
    for (const nickname of await shown.all()) {
        const whole = await nickname.evaluate(
            (element) =>
                element.getClientRects().length === 1 && element.scrollWidth <= element.clientWidth,
        );
        expect(whole).toBe(true);
    }
    // Nothing scrolls, and nothing is cut by the screen of the quiz either.
    const overflows = await page.evaluate(() => {
        const root = document.documentElement;
        const main = document.querySelector('main');
        return (
            root.scrollHeight > root.clientHeight ||
            root.scrollWidth > root.clientWidth ||
            !main ||
            main.scrollHeight > main.clientHeight ||
            main.scrollWidth > main.clientWidth
        );
    });
    expect(overflows).toBe(false);
});

test('/display/ waits for the game master once the server restarted with a game to resume', async ({
    page,
}) => {
    await serveDisplaySnapshot(page, fakeSnapshot([], advertisedAddress, 'ResumePending'));

    await page.goto('/display/');

    await expect(page.getByText(fr.display.resumePending)).toBeVisible();
    await expect(page.getByText(fr.display.scanToJoin)).toHaveCount(0);
});

/** The TV screen in a Chromium launched with `autoplayPolicy`, as on a PC or in kiosk mode. */
async function openDisplayWithAutoplay(
    chromium: BrowserType,
    baseURL: string | undefined,
    autoplayPolicy: 'user-gesture-required' | 'no-user-gesture-required',
): Promise<{ page: Page; browser: Browser }> {
    const browser = await chromium.launch({ args: [`--autoplay-policy=${autoplayPolicy}`] });
    const page = await browser.newPage({ baseURL, viewport: { width: 1920, height: 1080 } });
    await page.goto('/display/');
    return { page, browser };
}

test('/display/ on a PC shows « Démarrer » above the QR code until a click unlocks the audio', async ({
    playwright,
    baseURL,
}) => {
    const { page, browser } = await openDisplayWithAutoplay(
        playwright.chromium,
        baseURL,
        'user-gesture-required',
    );

    const start = page.getByRole('button', { name: fr.display.startAudio });
    await expect(start).toBeVisible();
    const qrCode = page.getByRole('img', { name: fr.display.qrCodeLabel });
    const [startBox, qrBox] = [await start.boundingBox(), await qrCode.boundingBox()];
    expect(startBox!.y + startBox!.height).toBeLessThanOrEqual(qrBox!.y);

    await start.click();

    await expect(start).toHaveCount(0);
    await browser.close();
});

test('/display/ in kiosk mode never shows « Démarrer »', async ({ playwright, baseURL }) => {
    const { page, browser } = await openDisplayWithAutoplay(
        playwright.chromium,
        baseURL,
        'no-user-gesture-required',
    );

    await expect(page.getByRole('img', { name: fr.display.qrCodeLabel })).toBeVisible();
    // The silence plays in a few milliseconds: long enough for the button to show otherwise.
    await page.waitForTimeout(500);
    await expect(page.getByRole('button', { name: fr.display.startAudio })).toHaveCount(0);
    await browser.close();
});

test('/display/ fits the reveal of an open question answered by 20 players on a 1080p screen', async ({
    page,
}) => {
    const players = longNicknames(20);
    const nicknames = players.map((player) => player.nickname);
    await serveImage(page, '/media/question');
    // 18 different answers of 25 characters, the first one by two authors, and one player
    // without answer.
    const groups = Array.from({ length: 18 }, (_, index) => ({
        text: `Réponse plutôt longue ${index + 1}`,
        correct: index < 5,
        nicknames: index === 0 ? nicknames.slice(0, 2) : nicknames.slice(index + 1, index + 2),
    }));
    const view: OpenQuestionDisplayView = {
        type: 'openquestion',
        questionNumber: 3,
        questionCount: 5,
        phase: 'Revealed',
        text: longText('Qui a peint', 200),
        imageUrl: '/media/question',
        answersCloseAt: null,
        answeredCount: 19,
        participantCount: 20,
        reveal: {
            expectedAnswer: 'Léonard de Vinci',
            groups,
            withoutAnswer: nicknames.slice(19),
        },
    };
    await serveDisplaySnapshot(page, fakeSnapshot(players, advertisedAddress, 'Round', view));

    await page.goto('/display/');

    const viewport = page.viewportSize();
    if (!viewport) {
        throw new Error('The test needs a fixed viewport');
    }
    const items = page.getByRole('listitem');
    await expect(items).toHaveCount(18);
    await page.screenshot({});
    for (const item of await items.all()) {
        await expect(item).toBeVisible();
        expectWithinSafeArea(await item.boundingBox(), viewport);
        const fontSize = await item.evaluate((element) =>
            parseFloat(getComputedStyle(element).fontSize),
        );
        // About 2.5 cm high on a 55" TV: readable from 3 m.
        expect(fontSize).toBeGreaterThanOrEqual(26);
    }
    expectWithinSafeArea(
        await page.getByText(fill(fr.modes.openquestion.display.unanswered, {})).boundingBox(),
        viewport,
    );
    const overflows = await page.evaluate(() => {
        const root = document.documentElement;
        const main = document.querySelector('main');
        return (
            root.scrollHeight > root.clientHeight ||
            !main ||
            main.scrollHeight > main.clientHeight ||
            main.scrollWidth > main.clientWidth
        );
    });
    expect(overflows).toBe(false);
});
