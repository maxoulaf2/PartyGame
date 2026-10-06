import { rmSync } from 'node:fs';
import { join } from 'node:path';
import { fileURLToPath } from 'node:url';
import type { Page } from '@playwright/test';
import { countText } from '../src/shared/i18n/countText.ts';
import { fill } from '../src/shared/i18n/fill.ts';
import { fr } from '../src/shared/i18n/fr.ts';
import { formatNumber } from '../src/shared/i18n/numberText.ts';
import { expect, test } from './fixtures/table.ts';

// The failures phase 3 makes invisible, provoked on a table of its own: the players and the TV
// screen must see nothing of them, the game master alone hears of them.

/**
 * A single pack, chosen at once: a first round whose question has an image, then a second one.
 * Both leave 120 s to answer, more than any of these scenarios takes.
 */
const chaosPacks = fileURLToPath(new URL('./chaos-packs', import.meta.url));

const question = 'Quelle est la capitale de l’Australie ?';

/** Long enough for a phone to find a server back: its attempts are up to 10 s apart. */
const reconnectTimeout = 30_000;

test.use({ serverPacks: chaosPacks });

async function startGame(gm: Page): Promise<void> {
    await gm.getByRole('button', { name: fr.gm.start.action, exact: true }).click();
    await gm
        .getByRole('dialog', { name: fr.gm.start.confirmTitle })
        .getByRole('button', { name: fr.gm.start.confirm, exact: true })
        .click();
    // The first round is announced first.
    await gm.getByRole('button', { name: fr.gm.startRound.action }).click();
}

/** The game master shows the question, then its two choices: the answers open. */
async function showWholeQuestion(gm: Page): Promise<void> {
    await gm.getByRole('button', { name: fr.modes.quiz.gm.showQuestion }).click();
    for (const letter of ['A', 'B']) {
        await gm
            .getByRole('button', { name: fill(fr.modes.quiz.gm.showChoice, { letter }) })
            .click();
    }
}

function choiceButton(phone: Page, letter: string) {
    return phone.getByRole('button', { name: fill(fr.modes.quiz.player.choiceLabel, { letter }) });
}

/** Chooses `letter` on `phone`, then checks that the server recorded it. */
async function answer(phone: Page, letter: string): Promise<void> {
    await choiceButton(phone, letter).click();
    await expect(phone.getByText(fr.modes.quiz.player.recorded)).toBeVisible();
    await expect(choiceButton(phone, letter)).toHaveAttribute('aria-pressed', 'true');
}

function answeredText(answered: number): string {
    return fill(fr.modes.quiz.answered, { answered, participants: 3 });
}

function earnedText(points: number): string {
    return fill(fr.modes.quiz.pointsEarned, { points: formatNumber(points) });
}

/** The row of a player in the list of the console. */
function consoleRow(gm: Page, nickname: string) {
    return gm
        .getByRole('list', { name: fr.gm.playerListLabel })
        .getByRole('listitem')
        .filter({ hasText: nickname });
}

/** The seconds left that the TV screen shows. */
async function secondsLeft(display: Page): Promise<number> {
    const text = (await display.getByRole('timer').textContent()) ?? '';
    return Number(/\d+$/.exec(text.trim())?.[0]);
}

/** Opens the incidents of the console, and checks that `message` is among them. */
async function expectIncident(gm: Page, message: string): Promise<void> {
    await gm.getByRole('button', { name: /incident/i }).click();
    await expect(gm.getByText(message)).toBeVisible();
}

/** Nothing tells the players nor the TV screen that something went wrong. */
async function expectNothingShown(pages: readonly Page[]): Promise<void> {
    for (const page of pages) {
        await expect(page.getByText(/incident|erreur|problème/i)).toHaveCount(0);
        await expect(page.getByText(fr.connection.reconnecting)).toHaveCount(0);
    }
}

test('a phone that loses the network answers offline: its answer waits, then counts once', async ({
    table,
}) => {
    const { display, gm } = table;
    const [zoe, max] = table.players;
    await startGame(gm);
    await showWholeQuestion(gm);
    await expect(choiceButton(max.page, 'B')).toBeEnabled();
    await expect(consoleRow(gm, max.nickname)).toContainText(fr.gm.connected);

    // The Wi-Fi of the phone drops silently: the server sees it leave, the phone does not know.
    await max.network.drop();
    await expect(consoleRow(gm, max.nickname)).toContainText(fr.gm.disconnected);

    // Its player answers: the choice shows at once, waiting for the server.
    await choiceButton(max.page, 'B').click();
    await expect(max.page.getByText(fr.modes.quiz.player.pending)).toBeVisible();
    await expect(choiceButton(max.page, 'B')).toHaveAttribute('aria-pressed', 'true');
    await expect(display.getByText(answeredText(0))).toBeVisible();

    // The network comes back: the phone sends the answer again, which counts once.
    await max.network.cut();
    await expect(max.page.getByText(fr.modes.quiz.player.recorded)).toBeVisible({
        timeout: reconnectTimeout,
    });
    await expect(consoleRow(gm, max.nickname)).toContainText(fr.gm.connected);
    await expect(display.getByText(answeredText(1))).toBeVisible();
    const answers = gm
        .getByRole('list', { name: fr.modes.quiz.gm.answersLabel })
        .getByRole('listitem');
    await expect(answers.filter({ hasText: max.nickname })).toHaveText(`${max.nickname} B`);

    // The other players answer: the answers lock, with three of them, not four.
    await answer(zoe.page, 'B');
    await answer(table.players[2].page, 'A');
    await expect(display.getByText(fr.modes.quiz.allAnswered)).toBeVisible();
    await expect(display.getByText(answeredText(3))).toBeVisible();
    await gm.getByRole('button', { name: fr.modes.quiz.gm.revealAnswer, exact: true }).click();
    await expect(max.page.getByText(earnedText(1000), { exact: true })).toBeVisible();
    await expectNothingShown([display, zoe.page, max.page]);
    // A lost connection is no incident: the console shows none.
    await expect(gm.getByRole('button', { name: /incident/i })).toHaveCount(0);
});

test('a question whose image is missing shows without it, and the console alone hears of it', async ({
    table,
    dedicatedServer,
}) => {
    const { display, gm, players } = table;
    await startGame(gm);
    rmSync(join(dedicatedServer.packDirectory, 'chaos', 'images', 'carte.png'));

    await gm.getByRole('button', { name: fr.modes.quiz.gm.showQuestion }).click();

    await expect(display.getByRole('heading', { name: question })).toBeVisible();
    await expect(display.getByRole('img', { name: fr.modes.quiz.display.imageLabel })).toHaveCount(
        0,
    );
    await gm
        .getByRole('button', { name: fill(fr.modes.quiz.gm.showChoice, { letter: 'A' }) })
        .click();
    await expect(
        display.getByRole('list', { name: fr.modes.quiz.choicesLabel }).getByRole('listitem'),
    ).toHaveText(['A Sydney']);
    await expectIncident(gm, fr.gm.incidents.codes.DisplayMediaFailed);
    await expectNothingShown([display, ...players.map((player) => player.page)]);
});

test('the server killed during a question: once resumed, every screen is back with the time left', async ({
    table,
    dedicatedServer,
}) => {
    test.setTimeout(120_000);
    const { display, gm } = table;
    const [zoe, max, lea] = table.players;
    await startGame(gm);
    await showWholeQuestion(gm);
    await answer(zoe.page, 'B');
    await expect(display.getByText(answeredText(1))).toBeVisible();
    const before = await secondsLeft(display);

    // Down long enough for a countdown that went on to show it.
    await dedicatedServer.kill();
    await new Promise((resolve) => setTimeout(resolve, 5_000));
    const code = '135790';
    await dedicatedServer.start([`--GameMaster:Code=${code}`]);

    // The game master reads the new code in the server console, then resumes the game.
    await expect(gm.getByText(fr.gm.code.expired)).toBeVisible({ timeout: reconnectTimeout });
    await gm.getByLabel(fr.gm.code.label).fill(code);
    await gm.getByRole('button', { name: fr.gm.code.submit }).click();
    await gm.getByRole('button', { name: fr.gm.resume.resume }).click();

    // The phones and the TV screen are back in the question by themselves, with the time it had.
    await expect(display.getByRole('heading', { name: question })).toBeVisible({
        timeout: reconnectTimeout,
    });
    const after = await secondsLeft(display);
    expect(after).toBeLessThanOrEqual(before);
    expect(after).toBeGreaterThan(before - 4);
    await expect(zoe.page.getByText(fr.modes.quiz.player.recorded)).toBeVisible({
        timeout: reconnectTimeout,
    });
    await expect(choiceButton(zoe.page, 'B')).toHaveAttribute('aria-pressed', 'true');
    await expect(display.getByText(answeredText(1))).toBeVisible();

    // The others answer, and the points are those of a game never stopped.
    for (const [player, letter] of [
        [max, 'B'],
        [lea, 'A'],
    ] as const) {
        await expect(choiceButton(player.page, letter)).toBeEnabled({ timeout: reconnectTimeout });
        await answer(player.page, letter);
    }
    await expect(display.getByText(fr.modes.quiz.allAnswered)).toBeVisible();
    await gm.getByRole('button', { name: fr.modes.quiz.gm.revealAnswer, exact: true }).click();
    for (const [player, points] of [
        [zoe, 1000],
        [max, 1000],
        [lea, 0],
    ] as const) {
        await expect(player.page.getByText(earnedText(points), { exact: true })).toBeVisible();
    }
    await expectNothingShown([display, zoe.page, max.page, lea.page]);
});

test.describe('with a fault injected in the answers', () => {
    test.use({
        serverSettings: [
            '--environment=Development',
            '--FaultInjection:FailOnInput=PlayerRoundInput',
            '--FaultInjection:FailCount=3',
        ],
    });

    test('only the console hears of it, and offers to skip the round at the third time', async ({
        table,
    }) => {
        const { display, gm, players } = table;
        const banner = gm.getByRole('alert').filter({ hasText: fr.gm.skipRound.problem });
        await startGame(gm);
        await showWholeQuestion(gm);

        // Each answer fails on the server: the phone forgets it, as the server never had it.
        for (const [index, player] of players.entries()) {
            await expect(banner).toHaveCount(0);
            await choiceButton(player.page, 'B').click();
            await expect(
                gm.getByRole('button', { name: countText(fr.gm.incidents.counter, index + 1) }),
            ).toBeVisible();
            await expect(choiceButton(player.page, 'B')).toHaveAttribute('aria-pressed', 'false');
            await expect(player.page.getByText(fr.modes.quiz.player.recorded)).toHaveCount(0);
        }
        await expect(display.getByText(answeredText(0))).toBeVisible();
        await expectNothingShown([display, ...players.map((player) => player.page)]);
        await expectIncident(gm, fr.gm.incidents.codes.RoundHandlerFailed);
        await gm.getByRole('button', { name: fr.gm.incidents.close }).click();

        // The game master skips the round, and the next one plays normally.
        await banner.getByRole('button', { name: fr.gm.skipRound.action }).click();
        await gm
            .getByRole('dialog', { name: fr.gm.skipRound.confirmTitle })
            .getByRole('button', { name: fr.gm.skipRound.confirm })
            .click();
        await expect(gm.getByText(fr.gm.skipRound.skipped)).toBeVisible();
        await gm.getByRole('button', { name: fr.gm.nextRound.action }).click();
        await gm.getByRole('button', { name: fr.gm.startRound.action }).click();
        await showWholeQuestion(gm);
        await answer(players[0].page, 'B');
        await expect(display.getByText(answeredText(1))).toBeVisible();
        await expectNothingShown([display, ...players.map((player) => player.page)]);
    });
});
