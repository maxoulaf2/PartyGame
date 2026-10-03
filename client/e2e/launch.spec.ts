import { expect, test, type Browser, type Page, type WebSocketRoute } from '@playwright/test';
import { gameMasterCodeKey } from '../src/shared/connection/codeStorage.ts';
import { countText } from '../src/shared/i18n/countText.ts';
import { fill } from '../src/shared/i18n/fill.ts';
import { fr } from '../src/shared/i18n/fr.ts';
import { formatNumber } from '../src/shared/i18n/numberText.ts';
import { rankText, standingText } from '../src/shared/i18n/rankText.ts';
import { gameMasterCode, playedPack } from './gameServer.ts';
import { joinOnNewPhone, uniqueNickname } from './players.ts';

// Starting the game cannot be undone, and every test shares the same server: playwright.config.ts
// runs this file in a project of its own, once all the other tests are done.

// The players of the other tests take part too, most of them gone: only the countdowns lock the
// answers, kept short by the pack (20 s, 15 s and 10 s), and waited for.
const countdownTimeout = 25_000;

async function openConsole(page: Page): Promise<void> {
    await page.addInitScript(
        ([key, code]) => {
            localStorage.setItem(key, code);
        },
        [gameMasterCodeKey, gameMasterCode] as const,
    );
    await page.goto('/gm/');
    await expect(page.getByRole('heading', { name: fr.gm.consoleTitle })).toBeVisible();
}

/** The points a player earned with a question, as the phones and the console show them. */
function earnedText(points: number): string {
    return fill(fr.modes.quiz.pointsEarned, { points: formatNumber(points) });
}

/** The total of a player in a ranking. */
function pointsText(points: number): string {
    return countText(fr.game.points, points);
}

function choicesOf(page: Page) {
    return page.getByRole('list', { name: fr.modes.quiz.choicesLabel }).getByRole('listitem');
}

/**
 * The phone is an answer pad: a button per choice, waiting for the answers to open, and neither
 * the question nor the texts of the choices, read on the TV screen.
 */
async function expectQuestionOnPhone(phone: Page, question: string, progress: string) {
    await expect(phone.getByText(progress)).toBeVisible();
    const buttons = choicesOf(phone).getByRole('button');
    await expect(buttons).toHaveCount(2);
    for (const letter of ['A', 'B']) {
        const button = phone.getByRole('button', {
            name: fill(fr.modes.quiz.player.choiceLabel, { letter }),
        });
        await expect(button).toBeDisabled();
        await expect(button).toHaveText(letter);
    }
    await expect(phone.getByText(question)).toHaveCount(0);
    await expect(phone.getByText('araignée')).toHaveCount(0);
}

test('the game master plays a whole game, from the choice of the pack to the final ranking', async ({
    page,
    browser,
    baseURL,
}) => {
    // Three countdowns run out during the game.
    test.setTimeout(120_000);
    const nickname = uniqueNickname('Zoé');
    const lateNickname = uniqueNickname('Max');
    const phone = await joinOnNewPhone(browser, baseURL, nickname);
    const display = await (await browser.newContext({ baseURL })).newPage();
    await display.goto('/display/');
    await openConsole(page);
    await expect(phone.getByText(fr.player.waiting)).toBeVisible();

    const start = page.getByRole('button', { name: fr.gm.start.action, exact: true });
    const dialog = page.getByRole('dialog', { name: fr.gm.start.confirmTitle });

    // The pack may already be chosen by the tests of the packs: choosing it again changes nothing.
    await page.getByRole('radio', { name: playedPack.title }).check();
    await expect(display.getByText(playedPack.title)).toBeVisible();

    // Cancelling leaves everything in the lobby.
    await start.click();
    await expect(dialog).toBeVisible();
    await dialog.getByRole('button', { name: fr.gm.start.cancel, exact: true }).click();
    await expect(dialog).toHaveCount(0);
    await expect(phone.getByText(fr.player.waiting)).toBeVisible();

    await start.click();
    await dialog.getByRole('button', { name: fr.gm.start.confirm, exact: true }).click();

    // The first question of the first round is presented on every interface, its correct answer
    // on the console only.
    const question = 'Combien de pattes a une araignée ?';
    const progress = fill(fr.modes.quiz.question, { number: 1, count: 3 });
    await expect(
        page.getByText(fill(fr.game.round, { number: 1, count: playedPack.rounds.length })),
    ).toBeVisible();
    await expect(page.getByRole('heading', { name: playedPack.rounds[0] })).toBeVisible();
    await expect(page.getByRole('heading', { name: question })).toBeVisible();
    await expect(choicesOf(page)).toHaveText(['A 6', `B 8 ${fr.modes.quiz.correct}`]);
    await expect(page.getByRole('button', { name: fr.modes.quiz.gm.openAnswers })).toBeVisible();
    await expect(page.getByRole('button', { name: fr.modes.quiz.gm.skipQuestion })).toBeVisible();
    await expect(start).toHaveCount(0);
    // The pack is fixed: neither the list of packs nor their reload are offered anymore.
    await expect(page.getByRole('radio')).toHaveCount(0);
    await expect(page.getByRole('button', { name: fr.gm.packs.reload })).toHaveCount(0);
    await expect(
        page.getByText(fr.gm.packs.played.replace('{title}', () => playedPack.title)),
    ).toBeVisible();

    await expect(display.getByText(playedPack.rounds[0], { exact: true })).toBeVisible();
    await expect(display.getByText(progress)).toBeVisible();
    await expect(display.getByRole('heading', { name: question })).toBeVisible();
    await expect(choicesOf(display)).toHaveText(['A 6', 'B 8']);

    await expectQuestionOnPhone(phone, question, progress);

    // Registration stays open: a late phone joins during the presentation, and sees the question.
    const latePhone = await joinOnNewPhone(browser, baseURL, lateNickname);
    await expectQuestionOnPhone(latePhone, question, progress);
    await expect(
        page
            .getByRole('list', { name: fr.gm.playerListLabel })
            .getByText(lateNickname, { exact: true }),
    ).toBeVisible();

    for (const other of [display, phone, latePhone]) {
        await expect(other.getByText(fr.modes.quiz.correct)).toHaveCount(0);
    }

    // Two more phones take part: one answers, the other lets the time run out.
    const thirdNickname = uniqueNickname('Léa');
    const silentNickname = uniqueNickname('Noé');
    const thirdPhone = await joinOnNewPhone(browser, baseURL, thirdNickname);
    const silentPhone = await joinOnNewPhone(browser, baseURL, silentNickname);
    await expectQuestionOnPhone(silentPhone, question, progress);

    // The game master opens the answers: the countdown starts on every screen.
    await page.getByRole('button', { name: fr.modes.quiz.gm.openAnswers }).click();
    const answers = page.getByRole('list', { name: fr.modes.quiz.gm.answersLabel });
    await expect(answers).toBeVisible();
    // Every registered player takes part, those of the other tests included, connected or not.
    const participants = await answers.getByRole('listitem').count();
    expect(participants).toBeGreaterThanOrEqual(4);
    for (const screen of [display, phone, page]) {
        await expect(screen.getByRole('timer')).toBeVisible();
    }
    await expect(display.getByText(answeredText(0, participants))).toBeVisible();

    // A phone that joins now plays from the next question.
    const tooLateNickname = uniqueNickname('Tom');
    const tooLatePhone = await joinOnNewPhone(browser, baseURL, tooLateNickname);
    await expect(tooLatePhone.getByText(fr.modes.quiz.player.nextQuestion)).toBeVisible();
    await expect(choiceButton(tooLatePhone, 'A')).toBeDisabled();

    // Three players answer: each choice shows at once, then is confirmed by the server.
    await answer(phone, 'B');
    await answer(latePhone, 'A');
    await answer(thirdPhone, 'B');
    await expect(display.getByText(answeredText(3, participants))).toBeVisible();
    // The TV screen tells how many answered, never what.
    await expect(display.getByText(nickname)).toHaveCount(0);

    await expect(answers.getByRole('listitem').filter({ hasText: nickname })).toHaveText(
        `${nickname} B`,
    );
    await expect(answers.getByRole('listitem').filter({ hasText: lateNickname })).toHaveText(
        `${lateNickname} A`,
    );
    await expect(choicesOf(page)).toHaveText([
        `A 6 ${countText(fr.modes.quiz.choiceAnswers, 1)}`,
        `B 8 ${fr.modes.quiz.correct} ${countText(fr.modes.quiz.choiceAnswers, 2)}`,
    ]);

    // The game master cannot lock the answers: the countdown does, the silent phone not answering.
    await expect(page.getByRole('button', { name: fr.modes.quiz.gm.revealAnswer })).toHaveCount(0);
    await expect(display.getByText(fr.modes.quiz.timeUp)).toBeVisible({
        timeout: countdownTimeout,
    });
    for (const screen of [display, silentPhone, page]) {
        await expect(screen.getByText(fr.modes.quiz.timeUp)).toBeVisible();
        await expect(screen.getByRole('timer')).toHaveCount(0);
    }
    await expect(display.getByText(answeredText(3, participants))).toBeVisible();
    // The phone that answered keeps its choice, recorded; the silent one shows none.
    await expect(phone.getByText(fr.modes.quiz.player.recorded)).toBeVisible();
    await expect(phone.getByText(fr.modes.quiz.timeUp)).toHaveCount(0);
    await expect(choiceButton(phone, 'B')).toHaveAttribute('aria-pressed', 'true');
    for (const letter of ['A', 'B']) {
        await expect(choiceButton(silentPhone, letter)).toHaveAttribute('aria-pressed', 'false');
        await expect(choiceButton(silentPhone, letter)).toBeDisabled();
    }

    // The game master reveals the answer: the TV screen shows who chose what, in order of arrival,
    // then the players taking part who did not answer.
    await page.getByRole('button', { name: fr.modes.quiz.gm.revealAnswer, exact: true }).click();
    await expect(chosenOnDisplay(display, 'B')).toHaveText([nickname, thirdNickname]);
    await expect(chosenOnDisplay(display, 'A')).toHaveText([lateNickname]);
    await expect(display.getByText(fr.modes.quiz.correct, { exact: true })).toBeVisible();
    const unanswered = display.getByRole('list', { name: fr.modes.quiz.display.unanswered });
    await expect(unanswered.getByText(silentNickname, { exact: true })).toBeVisible();
    await expect(display.getByText(tooLateNickname)).toHaveCount(0);

    // Each phone tells its own verdict, with the correct choice when its player missed it.
    const { verdicts } = fr.modes.quiz.player;
    for (const right of [phone, thirdPhone]) {
        await expect(right.getByText(verdicts.Correct, { exact: true })).toBeVisible();
    }
    await expect(latePhone.getByText(verdicts.Wrong, { exact: true })).toBeVisible();
    await expect(silentPhone.getByText(verdicts.NoAnswer, { exact: true })).toBeVisible();
    for (const missed of [latePhone, silentPhone]) {
        await expect(missed.getByText(fr.modes.quiz.player.correctChoice)).toBeVisible();
        await expect(correctChoiceOn(missed, 'B')).toBeVisible();
    }
    // Under the verdict, the points of the question and the new total: those of the round for a
    // correct answer, the pack giving no speed bonus, nothing otherwise.
    for (const right of [phone, thirdPhone]) {
        await expect(right.getByText(earnedText(1000), { exact: true })).toBeVisible();
        await expect(right.getByText(countText(fr.modes.quiz.player.score, 1000))).toBeVisible();
    }
    for (const missed of [latePhone, silentPhone]) {
        await expect(missed.getByText(earnedText(0), { exact: true })).toBeVisible();
        await expect(missed.getByText(countText(fr.modes.quiz.player.score, 0))).toBeVisible();
    }
    // The phone that joined too late to take part sees the correct answer, without verdict nor
    // points.
    await expect(correctChoiceOn(tooLatePhone, 'B')).toBeVisible();
    for (const verdict of Object.values(verdicts)) {
        await expect(tooLatePhone.getByText(verdict, { exact: true })).toHaveCount(0);
    }
    await expect(tooLatePhone.getByText(earnedText(0), { exact: true })).toHaveCount(0);

    // The console shows the same distribution, the points of each participant and every score,
    // and moving on comes next.
    await expect(choicesOf(page).filter({ hasText: fr.modes.quiz.correct })).toContainText(
        `${nickname} · ${thirdNickname}`,
    );
    await expect(answers.getByRole('listitem').filter({ hasText: nickname })).toHaveText(
        `${nickname} B ${earnedText(1000)}`,
    );
    await expect(answers.getByRole('listitem').filter({ hasText: lateNickname })).toHaveText(
        `${lateNickname} A ${earnedText(0)}`,
    );
    const players = page.getByRole('list', { name: fr.gm.playerListLabel }).getByRole('listitem');
    await expect(players.filter({ hasText: nickname })).toContainText(countText(fr.gm.score, 1000));
    await expect(players.filter({ hasText: tooLateNickname })).toContainText(
        countText(fr.gm.score, 0),
    );
    await expect(page.getByRole('button', { name: fr.modes.quiz.gm.revealAnswer })).toHaveCount(0);

    // The game master moves on: the second question is presented on every interface, and the
    // phone that joined too late for the first one takes part in it.
    await page.getByRole('button', { name: fr.modes.quiz.gm.nextQuestion }).click();
    const second = 'Quelle planète est la plus proche du Soleil ?';
    const secondProgress = fill(fr.modes.quiz.question, { number: 2, count: 3 });
    await expect(page.getByRole('heading', { name: second })).toBeVisible();
    await expect(display.getByRole('heading', { name: second })).toBeVisible();
    await expect(display.getByText(secondProgress)).toBeVisible();
    await expect(display.getByText(fr.modes.quiz.correct)).toHaveCount(0);
    for (const other of [phone, latePhone, thirdPhone, silentPhone, tooLatePhone]) {
        await expectQuestionOnPhone(other, second, secondProgress);
        await expect(correctChoiceOn(other, 'B')).toHaveCount(0);
    }
    await expect(tooLatePhone.getByText(fr.modes.quiz.player.nextQuestion)).toHaveCount(0);

    // The game master opens the answers, then skips the question after confirming: the third one
    // follows on every interface, numbered after the skipped one, and nothing tells about it.
    await page.getByRole('button', { name: fr.modes.quiz.gm.openAnswers }).click();
    await answer(tooLatePhone, 'A');
    const skipDialog = page.getByRole('dialog', {
        name: fill(fr.modes.quiz.gm.skipConfirm.title, { number: 2 }),
    });
    await page.getByRole('button', { name: fr.modes.quiz.gm.skipQuestion }).click();
    await skipDialog
        .getByRole('button', { name: fr.modes.quiz.gm.skipConfirm.confirm, exact: true })
        .click();
    const third = 'Combien de côtés a un hexagone ?';
    const thirdProgress = fill(fr.modes.quiz.question, { number: 3, count: 3 });
    await expect(page.getByRole('heading', { name: third })).toBeVisible();
    await expect(skipDialog).toHaveCount(0);
    await expect(display.getByRole('heading', { name: third })).toBeVisible();
    await expect(display.getByText(thirdProgress)).toBeVisible();
    for (const screen of [display, tooLatePhone]) {
        await expect(screen.getByRole('timer')).toHaveCount(0);
        await expect(screen.getByText(second)).toHaveCount(0);
    }
    await expectQuestionOnPhone(tooLatePhone, third, thirdProgress);
    for (const verdict of Object.values(verdicts)) {
        await expect(tooLatePhone.getByText(verdict, { exact: true })).toHaveCount(0);
    }

    // A phone loses its connection right after sending its answer, before the server acknowledges
    // it: once back, it sends the answer again, with the same number, and it counts once.
    const flakyNickname = uniqueNickname('Eva');
    const flaky = await joinWithRelayedNetwork(browser, baseURL, flakyNickname);
    await expectQuestionOnPhone(flaky.phone, third, thirdProgress);
    await page.getByRole('button', { name: fr.modes.quiz.gm.openAnswers }).click();
    // Listed once the console shows the opened answers: every participant is listed with it.
    await expect(answers.getByRole('listitem').filter({ hasText: flakyNickname })).toBeVisible();
    const thirdParticipants = await answers.getByRole('listitem').count();
    flaky.cutAfterNextIntent();
    await choiceButton(flaky.phone, 'B').click();
    await expect(flaky.phone.getByText(fr.modes.quiz.player.recorded)).toBeVisible();
    await expect(choiceButton(flaky.phone, 'B')).toHaveAttribute('aria-pressed', 'true');
    await expect.poll(() => flaky.sentClientSeqs()).toEqual([1, 1]);
    await expect(answers.getByRole('listitem').filter({ hasText: flakyNickname })).toHaveText(
        `${flakyNickname} B`,
    );
    await expect(display.getByText(answeredText(1, thirdParticipants))).toBeVisible();

    // The last question played, the console offers to end the round, which ends on every screen.
    await page
        .getByRole('button', { name: fr.modes.quiz.gm.revealAnswer, exact: true })
        .click({ timeout: countdownTimeout });
    await expect(page.getByRole('button', { name: fr.modes.quiz.gm.nextQuestion })).toHaveCount(0);
    await page.getByRole('button', { name: fr.modes.quiz.gm.endRound }).click();
    await expect(page.getByRole('button', { name: fr.gm.nextRound.action })).toBeVisible();
    await expect(page.getByRole('heading', { name: third })).toHaveCount(0);
    for (const screen of [display, phone]) {
        await expect(screen.getByText(thirdProgress)).toHaveCount(0);
    }

    // Between the two rounds, the TV screen ranks every player: the three who scored share the
    // first rank, in alphabetical order, and all the others, those of the other tests included,
    // share the fourth.
    const rankingAfter = fill(fr.game.rankingAfter, { number: 1 });
    await expect(display.getByRole('heading', { name: rankingAfter })).toBeVisible();
    const ranked = display.getByRole('list', { name: fr.game.rankingLabel }).getByRole('listitem');
    const playerCount = await ranked.count();
    expect(playerCount).toBeGreaterThanOrEqual(7);
    const first = rankText(fr.game.rank, 1);
    const fourth = rankText(fr.game.rank, 4);
    await expect(ranked.nth(0)).toHaveText(`${first} ${flakyNickname} ${pointsText(1000)}`);
    await expect(ranked.nth(1)).toHaveText(`${first} ${thirdNickname} ${pointsText(1000)}`);
    await expect(ranked.nth(2)).toHaveText(`${first} ${nickname} ${pointsText(1000)}`);
    await expect(ranked.filter({ hasText: lateNickname })).toHaveText(
        `${fourth} ${lateNickname} ${pointsText(0)}`,
    );
    // The TV screen still lets late arrivals join.
    await expect(display.getByRole('img', { name: fr.display.qrCodeLabel })).toBeVisible();

    // Each phone shows its own rank out of every player, and its score.
    const standings = [
        [phone, 1, 1000],
        [latePhone, 4, 0],
        [tooLatePhone, 4, 0],
    ] as const;
    for (const [other, rank, points] of standings) {
        await expect(
            other.getByText(
                standingText(fr.game.standing, fr.game.rank, { rank, isTied: true }, playerCount),
                { exact: true },
            ),
        ).toBeVisible();
        await expect(other.getByText(pointsText(points), { exact: true })).toBeVisible();
    }

    // The console shows the same ranking, and the title of the round to start next.
    await expect(page.getByRole('heading', { name: rankingAfter })).toBeVisible();
    await expect(
        page.getByRole('list', { name: fr.game.rankingLabel }).getByRole('listitem'),
    ).toHaveCount(playerCount);
    await expect(
        page.getByText(
            fill(fr.gm.nextRound.upcoming, {
                number: 2,
                count: playedPack.rounds.length,
                title: playedPack.rounds[1] ?? '',
            }),
        ),
    ).toBeVisible();

    // The game master starts the last round, of a single question: two players get it right.
    await page.getByRole('button', { name: fr.gm.nextRound.action }).click();
    const last = 'Quel est le plus grand océan ?';
    const lastProgress = fill(fr.modes.quiz.question, { number: 1, count: 1 });
    await expect(page.getByRole('heading', { name: playedPack.rounds[1] })).toBeVisible();
    await expect(display.getByRole('heading', { name: last })).toBeVisible();
    await expectQuestionOnPhone(phone, last, lastProgress);
    await page.getByRole('button', { name: fr.modes.quiz.gm.openAnswers }).click();
    await answer(phone, 'B');
    await answer(latePhone, 'B');
    await page
        .getByRole('button', { name: fr.modes.quiz.gm.revealAnswer, exact: true })
        .click({ timeout: countdownTimeout });
    await page.getByRole('button', { name: fr.modes.quiz.gm.endRound }).click();

    // The game is finished: the TV screen shows the podium, the first alone, the three second ex
    // aequo on the same step in alphabetical order, no third step, then every other player.
    await expect(display.getByRole('heading', { name: fr.game.finalRanking })).toBeVisible();
    const podium = display.getByRole('list', { name: fr.game.podiumLabel });
    const step = (rank: number) =>
        podium
            .getByRole('list', {
                name: fill(fr.game.podiumStepLabel, { rank: rankText(fr.game.rank, rank) }),
            })
            .getByRole('listitem');
    await expect(step(1)).toHaveText([nickname]);
    await expect(step(2)).toHaveText([flakyNickname, thirdNickname, lateNickname]);
    await expect(step(3)).toHaveCount(0);
    await expect(podium.locator('[data-rank="1"]')).toContainText(pointsText(2000));
    await expect(podium.locator('[data-rank="2"]')).toContainText(pointsText(1000));
    const rest = display.getByRole('list', { name: fr.game.restLabel }).getByRole('listitem');
    await expect(rest).toHaveCount(playerCount - 4);
    await expect(rest.filter({ hasText: silentNickname })).toHaveText(
        `${rankText(fr.game.rank, 5)} ${silentNickname} ${pointsText(0)}`,
    );

    // Each phone shows its final rank and score, with a word for those on the podium.
    const finalStandings = [
        [phone, { rank: 1, isTied: false }, 2000, fr.player.podium],
        [latePhone, { rank: 2, isTied: true }, 1000, fr.player.podium],
        [silentPhone, { rank: 5, isTied: true }, 0, fr.player.finished],
    ] as const;
    for (const [other, standing, points, message] of finalStandings) {
        await expect(other.getByRole('heading', { name: fr.game.finished })).toBeVisible();
        await expect(
            other.getByText(standingText(fr.game.standing, fr.game.rank, standing, playerCount), {
                exact: true,
            }),
        ).toBeVisible();
        await expect(other.getByText(pointsText(points), { exact: true })).toBeVisible();
        await expect(other.getByText(message, { exact: true })).toBeVisible();
    }

    // The console shows the final ranking, and nothing left to play.
    await expect(page.getByRole('heading', { name: fr.game.finished })).toBeVisible();
    const finalRanking = page
        .getByRole('list', { name: fr.game.rankingLabel })
        .getByRole('listitem');
    await expect(finalRanking).toHaveCount(playerCount);
    await expect(finalRanking.first()).toHaveText(
        `${rankText(fr.game.rank, 1)} ${nickname} ${pointsText(2000)}`,
    );
    await expect(page.getByRole('button', { name: fr.gm.nextRound.action })).toHaveCount(0);
    await expect(page.getByRole('button', { name: fr.modes.quiz.gm.openAnswers })).toHaveCount(0);

    // Registration stays open: a phone that joins now sees the end of the game, without a rank,
    // and the final ranking stays that of the game.
    const afterEndNickname = uniqueNickname('Ugo');
    const afterEndPhone = await joinOnNewPhone(browser, baseURL, afterEndNickname);
    await expect(afterEndPhone.getByRole('heading', { name: fr.game.finished })).toBeVisible();
    await expect(afterEndPhone.getByText(fr.player.joinedAfterEnd)).toBeVisible();
    await expect(
        page
            .getByRole('list', { name: fr.gm.playerListLabel })
            .getByText(afterEndNickname, { exact: true }),
    ).toBeVisible();
    await expect(finalRanking).toHaveCount(playerCount);
    await expect(display.getByText(afterEndNickname)).toHaveCount(0);

    await Promise.all(
        [
            phone,
            latePhone,
            thirdPhone,
            silentPhone,
            tooLatePhone,
            flaky.phone,
            afterEndPhone,
            display,
        ].map((other) => other.context().close()),
    );
});

/**
 * Joins on a new phone whose WebSockets are relayed by the test, which records the numbers of the
 * intents the phone sends, and can cut its connection right after it sends one: the server gets
 * the intent, the phone never gets its acknowledgment.
 */
async function joinWithRelayedNetwork(
    browser: Browser,
    baseURL: string | undefined,
    nickname: string,
) {
    const phone = await (await browser.newContext({ baseURL })).newPage();
    const clientSeqs: number[] = [];
    let cutting = false;
    await phone.routeWebSocket(/\/hub\/game/, (toPage: WebSocketRoute) => {
        const toServer = toPage.connectToServer();
        let cut = false;
        toServer.onMessage((message) => {
            // Once cut, nothing reaches the phone anymore: neither the acknowledgment nor snapshots.
            if (!cut) {
                toPage.send(message);
            }
        });
        toPage.onMessage((message) => {
            toServer.send(message);
            const seqs = intentClientSeqs(message);
            clientSeqs.push(...seqs);
            if (cutting && seqs.length > 0) {
                cutting = false;
                cut = true;
                void toServer.close().then(() => toPage.close());
            }
        });
    });
    await phone.goto('/');
    await phone.getByLabel(fr.player.join.label).fill(nickname);
    await phone.getByRole('button', { name: fr.player.join.submit }).click();
    await phone.getByLabel(fr.player.join.label).waitFor({ state: 'detached' });
    return {
        phone,
        /** Cuts the connection once the next intent is handed to the server. */
        cutAfterNextIntent: () => {
            cutting = true;
        },
        /** The numbers of the intents the phone sent, in order, sent again ones included. */
        sentClientSeqs: () => [...clientSeqs],
    };
}

/** The numbers of the intents a frame of the SignalR JSON protocol hands to the hub. */
function intentClientSeqs(message: string | Buffer): number[] {
    const seqs: number[] = [];
    for (const part of message.toString().split('\u001e')) {
        if (!part.includes('SendRoundIntent')) {
            continue;
        }
        const invocation = JSON.parse(part) as {
            target?: string;
            arguments?: { clientSeq?: number }[];
        };
        const clientSeq = invocation.arguments?.[0]?.clientSeq;
        if (invocation.target === 'SendRoundIntent' && clientSeq !== undefined) {
            seqs.push(clientSeq);
        }
    }
    return seqs;
}

function choiceButton(phone: Page, letter: string) {
    return phone.getByRole('button', { name: fill(fr.modes.quiz.player.choiceLabel, { letter }) });
}

/** The nicknames the TV screen shows under the choice `letter`, once revealed. */
function chosenOnDisplay(display: Page, letter: string) {
    return display
        .getByRole('list', { name: fill(fr.modes.quiz.display.choicePlayersLabel, { letter }) })
        .getByRole('listitem');
}

/** The correct choice as a phone shows it once revealed: its letter, its shape and its color. */
function correctChoiceOn(phone: Page, letter: string) {
    return phone.getByRole('img', { name: fill(fr.modes.quiz.player.choiceLabel, { letter }) });
}

function answeredText(answered: number, participants: number): string {
    return fill(fr.modes.quiz.answered, { answered, participants });
}

/** Chooses `letter` on `phone`, then checks that the server recorded it and nothing else is free. */
async function answer(phone: Page, letter: string) {
    await choiceButton(phone, letter).click();
    await expect(phone.getByText(fr.modes.quiz.player.recorded)).toBeVisible();
    await expect(choiceButton(phone, letter)).toHaveAttribute('aria-pressed', 'true');
    for (const button of await choicesOf(phone).getByRole('button').all()) {
        await expect(button).toBeDisabled();
    }
}
