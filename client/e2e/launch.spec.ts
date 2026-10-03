import { expect, test, type Page } from '@playwright/test';
import { gameMasterCodeKey } from '../src/shared/connection/codeStorage.ts';
import { countText } from '../src/shared/i18n/countText.ts';
import { fill } from '../src/shared/i18n/fill.ts';
import { fr } from '../src/shared/i18n/fr.ts';
import { gameMasterCode, playedPack } from './gameServer.ts';
import { joinOnNewPhone, uniqueNickname } from './players.ts';

// Starting the game cannot be undone, and every test shares the same server: playwright.config.ts
// runs this file in a project of its own, once all the other tests are done.

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

test('the game master starts the game, then plays the questions of its first round', async ({
    page,
    browser,
    baseURL,
}) => {
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

    // The game master locks the answers before the end of the countdown.
    await page.getByRole('button', { name: fr.modes.quiz.gm.lockAnswers }).click();
    for (const screen of [display, phone, silentPhone, page]) {
        await expect(screen.getByText(fr.modes.quiz.timeUp)).toBeVisible();
        await expect(screen.getByRole('timer')).toHaveCount(0);
    }
    await expect(display.getByText(answeredText(3, participants))).toBeVisible();
    // The phone that answered keeps its choice; the silent one shows none.
    await expect(choiceButton(phone, 'B')).toHaveAttribute('aria-pressed', 'true');
    for (const letter of ['A', 'B']) {
        await expect(choiceButton(silentPhone, letter)).toHaveAttribute('aria-pressed', 'false');
        await expect(choiceButton(silentPhone, letter)).toBeDisabled();
    }
    await expect(page.getByRole('button', { name: fr.modes.quiz.gm.lockAnswers })).toHaveCount(0);

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
    // The phone that joined too late to take part sees the correct answer, without verdict.
    await expect(correctChoiceOn(tooLatePhone, 'B')).toBeVisible();
    for (const verdict of Object.values(verdicts)) {
        await expect(tooLatePhone.getByText(verdict, { exact: true })).toHaveCount(0);
    }

    // The console shows the same distribution, and moving on comes next.
    await expect(choicesOf(page).filter({ hasText: fr.modes.quiz.correct })).toContainText(
        `${nickname} · ${thirdNickname}`,
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

    // The last question played, the console offers to end the round, which ends on every screen.
    await page.getByRole('button', { name: fr.modes.quiz.gm.openAnswers }).click();
    await page.getByRole('button', { name: fr.modes.quiz.gm.lockAnswers }).click();
    await page.getByRole('button', { name: fr.modes.quiz.gm.revealAnswer, exact: true }).click();
    await expect(page.getByRole('button', { name: fr.modes.quiz.gm.nextQuestion })).toHaveCount(0);
    await page.getByRole('button', { name: fr.modes.quiz.gm.endRound }).click();
    await expect(page.getByRole('button', { name: fr.gm.nextRound.action })).toBeVisible();
    await expect(page.getByRole('heading', { name: third })).toHaveCount(0);
    for (const screen of [display, phone]) {
        await expect(screen.getByText(thirdProgress)).toHaveCount(0);
    }

    await Promise.all(
        [phone, latePhone, thirdPhone, silentPhone, tooLatePhone, display].map((other) =>
            other.context().close(),
        ),
    );
});

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
