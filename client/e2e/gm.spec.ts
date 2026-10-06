import { expect, test, type Page } from '@playwright/test';
import { gameMasterCodeKey } from '../src/shared/connection/codeStorage.ts';
import type {
    GameId,
    GameMasterRoundView,
    GameMasterSavedGame,
    GameMasterSnapshot,
    IncidentList,
    PlayerId,
    QuizGameMasterView,
    RoundId,
    RoundInfo,
} from '../src/shared/contracts';
import { countText } from '../src/shared/i18n/countText.ts';
import { fill, roundText } from '../src/shared/i18n/fill.ts';
import { fr } from '../src/shared/i18n/fr.ts';
import { rankText } from '../src/shared/i18n/rankText.ts';
import { serveGameMasterSnapshot } from './fakeHub.ts';
import { gameMasterCode } from './gameServer.ts';
import { trackExternalRequests } from './localRequests.ts';
import { joinOnNewPhone, uniqueNickname } from './players.ts';

/** Opens the GM console with the right code already remembered, as after a first visit. */
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

/** A lobby without any player, on a host whose networks are `candidates`. */
function fakeLobby(
    candidates: GameMasterSnapshot['joinAddressCandidates'] = [
        { address: '192.168.1.42', interfaceName: 'Wi-Fi' },
    ],
): GameMasterSnapshot {
    return {
        gameId: '6f9619ff-8b86-d011-b42d-00cf4fc964ff' as GameId,
        version: 1,
        phase: 'Lobby',
        players: [],
        minimumPlayerCount: 1,
        joinAddress: candidates[0]?.address ?? null,
        joinAddressCandidates: candidates,
        packCatalog: { directory: '/srv/partygame/packs', packs: [] },
        selectedPackId: null,
        packTitle: null,
        round: null,
        roundView: null,
        ranking: [],
        nextRoundTitle: null,
        roundSkipped: false,
        savedGame: null,
        joinCodeShown: false,
        preview: null,
    };
}

const firstRound: RoundInfo = {
    roundId: '0f8fad5b-d9cb-469f-a165-70867728950e' as RoundId,
    number: 1,
    count: 3,
    title: 'Échauffement',
    mode: 'quiz',
    description: null,
};

/** The first of three rounds in progress, shown by `roundView`. */
function fakeRound(roundView: GameMasterRoundView): GameMasterSnapshot {
    return {
        ...fakeLobby(),
        phase: 'Round',
        packCatalog: null,
        selectedPackId: 'soiree',
        packTitle: 'Grande soirée',
        round: firstRound,
        roundView,
    };
}

function addressOption(address: string, origin: string): string {
    return fr.gm.address.option
        .replace('{address}', () => address)
        .replace('{origin}', () => origin);
}

function withNickname(text: string, nickname: string): string {
    return text.replace('{nickname}', () => nickname);
}

/** Records the target of every hub message the page receives. */
function trackHubMessages(page: Page): string[] {
    const targets: string[] = [];
    page.on('websocket', (socket) => {
        socket.on('framereceived', ({ payload }) => {
            // The JSON protocol separates messages with the 0x1e record separator.
            for (const message of String(payload).split('\u001e')) {
                const target = /"target":"([^"]+)"/.exec(message)?.[1];
                if (target) {
                    targets.push(target);
                }
            }
        });
    });
    return targets;
}

test('/gm/ asks for the code, refuses a wrong one, then remembers the right one', async ({
    page,
}) => {
    const external = trackExternalRequests(page);
    const received = trackHubMessages(page);
    await page.goto('/gm/');

    const field = page.getByLabel(fr.gm.code.label);
    const submit = page.getByRole('button', { name: fr.gm.code.submit });
    await expect(field).toHaveAttribute('inputmode', 'numeric');
    await field.fill('12345');
    await expect(submit).toBeDisabled();

    await field.fill('000000');
    await expect(submit).toBeEnabled();
    await submit.click();

    await expect(page.getByText(fr.gm.code.invalid)).toBeVisible();
    await expect(field).toHaveValue('');
    await expect(field).toBeFocused();
    expect(received).not.toContain('ReceiveGameMasterSnapshot');

    await field.fill(` ${gameMasterCode} `);
    await submit.click();

    await expect(page.getByRole('heading', { name: fr.gm.consoleTitle })).toBeVisible();
    expect(received).toContain('ReceiveGameMasterSnapshot');

    await page.reload();

    await expect(page.getByRole('heading', { name: fr.gm.consoleTitle })).toBeVisible();
    await expect(page.getByLabel(fr.gm.code.label)).toHaveCount(0);
    expect(external).toEqual([]);
});

test('/gm/ asks for the code again when the remembered one is no longer valid', async ({
    page,
}) => {
    await page.addInitScript((key) => {
        localStorage.setItem(key, '000000');
    }, gameMasterCodeKey);

    await page.goto('/gm/');

    await expect(page.getByText(fr.gm.code.expired)).toBeVisible();
    await expect(page.getByLabel(fr.gm.code.label)).toBeVisible();
    expect(await page.evaluate((key) => localStorage.getItem(key), gameMasterCodeKey)).toBeNull();
});

test('/gm/ lists the players and renames one on every interface', async ({
    page,
    browser,
    baseURL,
}) => {
    const nickname = uniqueNickname('Zoé');
    const renamed = uniqueNickname('Léa');
    const phone = await joinOnNewPhone(browser, baseURL, nickname);
    const display = await (await browser.newContext({ baseURL })).newPage();
    await display.goto('/display/');
    await openConsole(page);

    const players = page.getByRole('list', { name: fr.gm.playerListLabel });
    const row = players.getByRole('listitem').filter({ hasText: nickname });
    await expect(row).toContainText(fr.gm.connected);

    await row.getByRole('button', { name: withNickname(fr.gm.rename.actionFor, nickname) }).click();
    const field = page.getByLabel(withNickname(fr.gm.rename.label, nickname));
    const submit = page.getByRole('button', { name: fr.gm.rename.submit });

    // An invisible character only the server spots: refused, and the field keeps what was typed.
    await field.fill('Zo​é');
    await submit.click();
    await expect(page.getByText(fr.gm.rename.problems.invalid)).toBeVisible();
    await expect(field).toHaveValue('Zo​é');

    await field.fill(renamed);
    await submit.click();

    await expect(field).toHaveCount(0);
    await expect(players.getByText(renamed, { exact: true })).toBeVisible();
    await expect(players.getByText(nickname, { exact: true })).toHaveCount(0);
    await expect(
        display
            .getByRole('list', { name: fr.display.playerListLabel })
            .getByText(renamed, { exact: true }),
    ).toBeVisible();
    await expect(phone.getByText(withNickname(fr.player.registeredAs, renamed))).toBeVisible();

    await phone.context().close();
    await display.context().close();
});

test('/gm/ shows a player whose phone left as disconnected', async ({ page, browser, baseURL }) => {
    const nickname = uniqueNickname('Max');
    const phone = await joinOnNewPhone(browser, baseURL, nickname);
    await openConsole(page);
    const row = page
        .getByRole('list', { name: fr.gm.playerListLabel })
        .getByRole('listitem')
        .filter({ hasText: nickname });
    await expect(row).toContainText(fr.gm.connected);

    await phone.context().close();

    await expect(row).toContainText(fr.gm.disconnected);
});

test('/gm/ cannot start a game without any player nor pack, and says what is missing', async ({
    page,
}) => {
    await serveGameMasterSnapshot(page, fakeLobby());

    await openConsole(page);

    await expect(page.getByRole('button', { name: fr.gm.start.action })).toBeDisabled();
    await expect(page.getByText(countText(fr.gm.start.minimumPlayers, 1))).toBeVisible();
    await expect(page.getByText(fr.gm.start.packRequired)).toBeVisible();
});

test('/gm/ says when no pack was found, with the folder it read', async ({ page }) => {
    await serveGameMasterSnapshot(page, fakeLobby());

    await openConsole(page);

    await expect(page.getByText(fr.gm.packs.none, { exact: true })).toBeVisible();
    await expect(
        page.getByText(fr.gm.packs.directory.replace('{directory}', () => '/srv/partygame/packs')),
    ).toBeVisible();
    await expect(page.getByRole('radio')).toHaveCount(0);
});

test('/gm/ shows the advertised address without a choice when the host has one network', async ({
    page,
}) => {
    await serveGameMasterSnapshot(page, fakeLobby());

    await openConsole(page);

    await expect(page.getByText(addressOption('192.168.1.42', 'Wi-Fi'))).toBeVisible();
    await expect(page.getByRole('combobox')).toHaveCount(0);
});

test('/gm/ lets the game master choose the address among the networks of the host', async ({
    page,
}) => {
    const hub = await serveGameMasterSnapshot(
        page,
        fakeLobby([
            { address: '192.168.50.7', interfaceName: null },
            { address: '192.168.1.42', interfaceName: 'Wi-Fi' },
            { address: '10.0.0.2', interfaceName: 'Ethernet' },
        ]),
    );

    await openConsole(page);

    const select = page.getByLabel(fr.gm.address.label);
    await expect(select).toHaveValue('192.168.50.7');
    await expect(select.getByRole('option')).toHaveText([
        addressOption('192.168.50.7', fr.gm.address.configured),
        addressOption('192.168.1.42', 'Wi-Fi'),
        addressOption('10.0.0.2', 'Ethernet'),
    ]);

    await select.selectOption('10.0.0.2');

    await expect(select).toHaveValue('10.0.0.2');
    await expect(select).toBeEnabled();
    expect(hub.chosen).toEqual(['10.0.0.2']);
});

test('/gm/ presents the question of the round in progress, its correct answer marked', async ({
    page,
}) => {
    const view: QuizGameMasterView = {
        type: 'quiz',
        questionNumber: 2,
        questionCount: 5,
        phase: 'Presentation',
        text: 'Quelle est la capitale de l’Australie ?',
        questionShown: true,
        choices: [
            { letter: 'A', text: 'Sydney', correct: false, shown: false, answerCount: 0 },
            { letter: 'B', text: 'Canberra', correct: true, shown: false, answerCount: 0 },
            { letter: 'C', text: 'Melbourne', correct: false, shown: false, answerCount: 0 },
        ],
        answersCloseAt: null,
        answers: [],
    };
    await serveGameMasterSnapshot(page, fakeRound(view));

    await openConsole(page);

    const round = firstRound;
    await expect(page.getByText(roundText(fr.game.round, round))).toBeVisible();
    await expect(page.getByRole('heading', { name: round.title })).toBeVisible();
    await expect(
        page.getByText(fill(fr.modes.quiz.question, { number: 2, count: 5 })),
    ).toBeVisible();
    await expect(page.getByRole('heading', { name: view.text })).toBeVisible();
    const choices = page
        .getByRole('list', { name: fr.modes.quiz.choicesLabel })
        .getByRole('listitem');
    // Told by a label, never by the color alone: only the correct choice has it.
    await expect(choices).toHaveText([
        `A Sydney ${fr.modes.quiz.gm.hiddenOnDisplay}`,
        `B Canberra ${fr.modes.quiz.correct} ${fr.modes.quiz.gm.hiddenOnDisplay}`,
        `C Melbourne ${fr.modes.quiz.gm.hiddenOnDisplay}`,
    ]);
    await expect(
        page.getByRole('button', { name: fill(fr.modes.quiz.gm.showChoice, { letter: 'A' }) }),
    ).toBeEnabled();
    await expect(page.getByRole('button', { name: fr.modes.quiz.gm.skipQuestion })).toBeEnabled();
    // Nothing to count before the answers open, with the first choice shown.
    await expect(page.getByRole('list', { name: fr.modes.quiz.gm.answersLabel })).toHaveCount(0);
    await expect(page.getByRole('timer')).toHaveCount(0);
    await expect(page.getByRole('button', { name: fr.gm.start.action })).toHaveCount(0);
});

/**
 * The question of the round in progress in presentation, the TV screen showing its first
 * `shownCount` choices, and the players its answers wait for once the first one shows.
 */
function presentationView(
    questionShown: boolean,
    shownCount: number,
    answers: QuizGameMasterView['answers'] = [],
): QuizGameMasterView {
    return {
        type: 'quiz',
        questionNumber: 2,
        questionCount: 5,
        phase: 'Presentation',
        text: 'Quelle est la capitale de l’Australie ?',
        questionShown,
        choices: (
            [
                ['A', 'Sydney', false],
                ['B', 'Canberra', true],
                ['C', 'Melbourne', false],
            ] as const
        ).map(([letter, text, correct], index) => ({
            letter,
            text,
            correct,
            shown: index < shownCount,
            answerCount: answers.filter((answer) => answer.choice === letter).length,
        })),
        answersCloseAt: null,
        answers,
    };
}

test('/gm/ reads out the question before showing it on the TV screen', async ({ page }) => {
    const hub = await serveGameMasterSnapshot(page, fakeRound(presentationView(false, 0)));

    await openConsole(page);

    // The console shows the whole question at once, marking what the TV screen does not show.
    const question = page.getByRole('heading', { name: 'Quelle est la capitale de l’Australie ?' });
    await expect(question).toBeVisible();
    await expect(page.getByText(fr.modes.quiz.gm.hiddenOnDisplay)).toHaveCount(4);
    await expect(
        page.getByRole('list', { name: fr.modes.quiz.choicesLabel }).getByRole('listitem'),
    ).toHaveText([
        `A Sydney ${fr.modes.quiz.gm.hiddenOnDisplay}`,
        `B Canberra ${fr.modes.quiz.correct} ${fr.modes.quiz.gm.hiddenOnDisplay}`,
        `C Melbourne ${fr.modes.quiz.gm.hiddenOnDisplay}`,
    ]);
    await expect(
        page.getByRole('button', { name: fill(fr.modes.quiz.gm.showChoice, { letter: 'A' }) }),
    ).toHaveCount(0);

    await page.getByRole('button', { name: fr.modes.quiz.gm.showQuestion }).click();

    await expect
        .poll(() => hub.roundIntents)
        .toEqual([{ type: 'quiz.showQuestion', roundId: firstRound.roundId, questionNumber: 2 }]);
});

test('/gm/ shows the next choice on the TV screen once read out', async ({ page }) => {
    const hub = await serveGameMasterSnapshot(page, fakeRound(presentationView(true, 1)));

    await openConsole(page);

    await expect(page.getByText(fr.modes.quiz.gm.hiddenOnDisplay)).toHaveCount(2);
    await expect(page.getByRole('button', { name: fr.modes.quiz.gm.showQuestion })).toHaveCount(0);
    await page
        .getByRole('button', { name: fill(fr.modes.quiz.gm.showChoice, { letter: 'B' }) })
        .click();

    // The intent names the choice it shows: sent twice, it shows a single one.
    await expect
        .poll(() => hub.roundIntents)
        .toEqual([
            {
                type: 'quiz.showChoice',
                roundId: firstRound.roundId,
                questionNumber: 2,
                choice: 'B',
            },
        ]);
});

test('/gm/ waits neutrally on a round of a mode it does not know', async ({ page }) => {
    await serveGameMasterSnapshot(
        page,
        fakeRound({ type: 'karaoke' } as unknown as GameMasterRoundView),
    );

    await openConsole(page);

    await expect(page.getByText(fr.gm.inProgress, { exact: true })).toBeVisible();
    await expect(page.getByText('karaoke')).toHaveCount(0);
    // The rest of the console stays usable: the players, and the address of the QR code.
    await expect(page.getByText(fr.gm.address.label)).toBeVisible();
});

/** The question of the round in progress, its answers open, as the console shows it. */
function answeringView(answers: QuizGameMasterView['answers']): QuizGameMasterView {
    const countOf = (letter: string) => answers.filter((answer) => answer.choice === letter).length;
    return {
        type: 'quiz',
        questionNumber: 2,
        questionCount: 5,
        phase: 'Answering',
        text: 'Quelle est la capitale de l’Australie ?',
        questionShown: true,
        choices: [
            { letter: 'A', text: 'Sydney', correct: false, shown: true, answerCount: countOf('A') },
            {
                letter: 'B',
                text: 'Canberra',
                correct: true,
                shown: true,
                answerCount: countOf('B'),
            },
        ],
        answersCloseAt: Date.now() + 20_000,
        answers,
    };
}

const zoe = '6f9619ff-8b86-d011-b42d-00cf4fc964ff' as PlayerId;
const max = '7c9e6679-7425-40de-944b-e07fc1f90ae7' as PlayerId;

test('/gm/ follows what each player answers while the answers are open', async ({ page }) => {
    const view = answeringView([
        { playerId: zoe, nickname: 'Zoé', choice: 'B', points: null },
        { playerId: max, nickname: 'Max', choice: null, points: null },
    ]);
    await serveGameMasterSnapshot(page, fakeRound(view));

    await openConsole(page);

    await expect(page.getByRole('timer')).toBeVisible();
    await expect(
        page.getByText(fill(fr.modes.quiz.answered, { answered: 1, participants: 2 })),
    ).toBeVisible();
    await expect(page.getByText(fr.modes.quiz.allAnswered)).toHaveCount(0);
    await expect(
        page.getByRole('list', { name: fr.modes.quiz.gm.answersLabel }).getByRole('listitem'),
    ).toHaveText(['Zoé B', `Max ${fr.modes.quiz.gm.waitingAnswer}`]);
    // The distribution, live.
    await expect(
        page.getByRole('list', { name: fr.modes.quiz.choicesLabel }).getByRole('listitem'),
    ).toHaveText([
        `A Sydney ${countText(fr.modes.quiz.choiceAnswers, 0)}`,
        `B Canberra ${fr.modes.quiz.correct} ${countText(fr.modes.quiz.choiceAnswers, 1)}`,
    ]);
    // Only the countdown or the last answer locks them: the game master can only skip the question.
    await expect(page.getByRole('button', { name: fr.modes.quiz.gm.skipQuestion })).toBeEnabled();
    await expect(page.getByRole('button', { name: fr.modes.quiz.gm.revealAnswer })).toHaveCount(0);
});

test('/gm/ follows the answers given while the choices show, before any countdown', async ({
    page,
}) => {
    const view = presentationView(true, 2, [
        { playerId: zoe, nickname: 'Zoé', choice: 'B', points: null },
        { playerId: max, nickname: 'Max', choice: null, points: null },
    ]);
    await serveGameMasterSnapshot(page, fakeRound(view));

    await openConsole(page);

    await expect(
        page.getByText(fill(fr.modes.quiz.answered, { answered: 1, participants: 2 })),
    ).toBeVisible();
    await expect(
        page.getByRole('list', { name: fr.modes.quiz.gm.answersLabel }).getByRole('listitem'),
    ).toHaveText(['Zoé B', `Max ${fr.modes.quiz.gm.waitingAnswer}`]);
    // Counted under the choices shown only: the players cannot choose the others yet.
    await expect(
        page.getByRole('list', { name: fr.modes.quiz.choicesLabel }).getByRole('listitem'),
    ).toHaveText([
        `A Sydney ${countText(fr.modes.quiz.choiceAnswers, 0)}`,
        `B Canberra ${fr.modes.quiz.correct} ${countText(fr.modes.quiz.choiceAnswers, 1)}`,
        `C Melbourne ${fr.modes.quiz.gm.hiddenOnDisplay}`,
    ]);
    // The countdown starts with the last choice.
    await expect(page.getByRole('timer')).toHaveCount(0);
    await expect(
        page.getByRole('button', { name: fill(fr.modes.quiz.gm.showChoice, { letter: 'C' }) }),
    ).toBeEnabled();
});

test('/gm/ tells when every player taking part answered, which locks the answers', async ({
    page,
}) => {
    const view = answeringView([
        { playerId: zoe, nickname: 'Zoé', choice: 'B', points: null },
        { playerId: max, nickname: 'Max', choice: 'A', points: null },
    ]);
    await serveGameMasterSnapshot(
        page,
        fakeRound({ ...view, phase: 'Locked', answersCloseAt: null }),
    );

    await openConsole(page);

    await expect(page.getByText(fr.modes.quiz.allAnswered)).toBeVisible();
    await expect(page.getByText(fr.modes.quiz.timeUp)).toHaveCount(0);
    await expect(page.getByRole('button', { name: fr.modes.quiz.gm.revealAnswer })).toBeEnabled();
});

test('/gm/ offers to reveal the answer once the countdown locked the answers', async ({ page }) => {
    const view = answeringView([
        { playerId: zoe, nickname: 'Zoé', choice: 'B', points: null },
        { playerId: max, nickname: 'Max', choice: null, points: null },
    ]);
    await serveGameMasterSnapshot(
        page,
        fakeRound({ ...view, phase: 'Locked', answersCloseAt: null }),
    );

    await openConsole(page);

    await expect(page.getByText(fr.modes.quiz.timeUp)).toBeVisible();
    await expect(page.getByText(fr.modes.quiz.allAnswered)).toHaveCount(0);
    await expect(page.getByRole('button', { name: fr.modes.quiz.gm.revealAnswer })).toBeEnabled();
});

test('/gm/ shows who chose what once the answer is revealed', async ({ page }) => {
    const view = answeringView([
        { playerId: zoe, nickname: 'Zoé', choice: 'B', points: null },
        { playerId: max, nickname: 'Max', choice: null, points: null },
    ]);
    await serveGameMasterSnapshot(
        page,
        fakeRound({ ...view, phase: 'Revealed', answersCloseAt: null }),
    );

    await openConsole(page);

    await expect(
        page.getByRole('list', { name: fr.modes.quiz.choicesLabel }).getByRole('listitem'),
    ).toHaveText([
        `A Sydney ${countText(fr.modes.quiz.choiceAnswers, 0)}`,
        `B Canberra ${fr.modes.quiz.correct} ${countText(fr.modes.quiz.choiceAnswers, 1)} Zoé`,
    ]);
    await expect(
        page.getByRole('list', { name: fr.modes.quiz.gm.answersLabel }).getByRole('listitem'),
    ).toHaveText(['Zoé B', `Max ${fr.modes.quiz.noAnswer}`]);
    await expect(page.getByRole('button', { name: fr.modes.quiz.gm.revealAnswer })).toHaveCount(0);
    await expect(page.getByRole('button', { name: fr.modes.quiz.gm.skipQuestion })).toHaveCount(0);
    await expect(page.getByRole('button', { name: fr.modes.quiz.gm.nextQuestion })).toBeEnabled();
    // Not the last question: the round goes on.
    await expect(page.getByRole('button', { name: fr.modes.quiz.gm.endRound })).toHaveCount(0);
});

test('/gm/ moves on from the revealed question to the next one', async ({ page }) => {
    const view = answeringView([{ playerId: zoe, nickname: 'Zoé', choice: 'B', points: null }]);
    const hub = await serveGameMasterSnapshot(
        page,
        fakeRound({ ...view, phase: 'Revealed', answersCloseAt: null }),
    );

    await openConsole(page);
    await page.getByRole('button', { name: fr.modes.quiz.gm.nextQuestion }).click();

    // The intent names the question it moves on from: sent twice, it moves on only once.
    await expect
        .poll(() => hub.roundIntents)
        .toEqual([{ type: 'quiz.nextQuestion', roundId: firstRound.roundId, questionNumber: 2 }]);
});

test('/gm/ ends the round once its last question is revealed', async ({ page }) => {
    const view = answeringView([{ playerId: zoe, nickname: 'Zoé', choice: 'B', points: null }]);
    const hub = await serveGameMasterSnapshot(
        page,
        fakeRound({ ...view, questionNumber: 5, phase: 'Revealed', answersCloseAt: null }),
    );

    await openConsole(page);
    await expect(page.getByRole('button', { name: fr.modes.quiz.gm.nextQuestion })).toHaveCount(0);
    await page.getByRole('button', { name: fr.modes.quiz.gm.endRound }).click();

    await expect
        .poll(() => hub.roundIntents)
        .toEqual([{ type: 'quiz.nextQuestion', roundId: firstRound.roundId, questionNumber: 5 }]);
});

test('/gm/ skips the question in progress once the game master confirms', async ({ page }) => {
    const view = answeringView([{ playerId: zoe, nickname: 'Zoé', choice: 'B', points: null }]);
    const hub = await serveGameMasterSnapshot(page, fakeRound(view));
    const skip = page.getByRole('button', { name: fr.modes.quiz.gm.skipQuestion });
    const dialog = page.getByRole('dialog', {
        name: fill(fr.modes.quiz.gm.skipConfirm.title, { number: 2 }),
    });

    await openConsole(page);

    // Cancelling sends nothing.
    await skip.click();
    await expect(dialog).toBeVisible();
    await expect(dialog.getByText(fr.modes.quiz.gm.skipConfirm.message)).toBeVisible();
    await dialog.getByRole('button', { name: fr.modes.quiz.gm.skipConfirm.cancel }).click();
    await expect(dialog).toHaveCount(0);

    await skip.click();
    await dialog.getByRole('button', { name: fr.modes.quiz.gm.skipConfirm.confirm }).click();

    await expect(dialog).toHaveCount(0);
    await expect
        .poll(() => hub.roundIntents)
        .toEqual([{ type: 'quiz.skipQuestion', roundId: firstRound.roundId, questionNumber: 2 }]);
});

test('/gm/ shows then hides the QR code on the TV screen during a round', async ({ page }) => {
    await serveGameMasterSnapshot(page, fakeRound(answeringView([])));
    const show = page.getByRole('button', { name: fr.gm.joinCode.show });
    const hide = page.getByRole('button', { name: fr.gm.joinCode.hide });

    await openConsole(page);

    await show.click();
    await expect(hide).toHaveAttribute('aria-pressed', 'true');
    await hide.click();
    await expect(show).toHaveAttribute('aria-pressed', 'false');
});

/** The incidents of a round whose inputs failed `count` times, failing from the third one. */
function failures(count: number): IncidentList {
    return {
        version: count,
        incidents: [
            {
                id: 1,
                code: 'RoundHandlerFailed',
                round: firstRound,
                role: null,
                step: null,
                count,
                lastOccurredAt: 1_790_000_000_000,
            },
        ],
        failingRounds: count >= 3 ? [firstRound.roundId] : [],
    };
}

test('/gm/ offers to skip a round that keeps failing, then tells it was skipped', async ({
    page,
}) => {
    const hub = await serveGameMasterSnapshot(page, fakeRound(answeringView([])), failures(3));
    const banner = page.getByRole('alert').filter({ hasText: fr.gm.skipRound.problem });
    const dialog = page.getByRole('dialog', { name: fr.gm.skipRound.confirmTitle });

    await openConsole(page);
    await expect(banner).toBeVisible();

    // Cancelling sends nothing.
    await banner.getByRole('button', { name: fr.gm.skipRound.action }).click();
    await expect(dialog.getByText(fr.gm.skipRound.confirmMessage)).toBeVisible();
    await dialog.getByRole('button', { name: fr.gm.skipRound.cancel }).click();
    await expect(dialog).toHaveCount(0);

    await banner.getByRole('button', { name: fr.gm.skipRound.action }).click();
    await dialog.getByRole('button', { name: fr.gm.skipRound.confirm }).click();

    await expect.poll(() => hub.skipped).toEqual([firstRound.roundId]);
    await expect(page.getByText(roundText(fr.game.roundEnded, firstRound))).toBeVisible();
    await expect(page.getByText(fr.gm.skipRound.skipped)).toBeVisible();
    await expect(banner).toHaveCount(0);
});

test('/gm/ goes back to the lobby during a round once confirmed', async ({ page }) => {
    const snapshot = fakeRound(answeringView([]));
    const hub = await serveGameMasterSnapshot(page, snapshot);
    const dialog = page.getByRole('dialog', { name: fr.gm.returnToLobby.confirmTitle });

    await openConsole(page);

    // Cancelling sends nothing.
    await page.getByRole('button', { name: fr.gm.returnToLobby.action }).click();
    await expect(dialog.getByText(fr.gm.returnToLobby.confirmMessage)).toBeVisible();
    await dialog.getByRole('button', { name: fr.gm.returnToLobby.cancel }).click();
    await expect(dialog).toHaveCount(0);
    expect(hub.returned).toEqual([]);

    await page.getByRole('button', { name: fr.gm.returnToLobby.action }).click();
    await dialog.getByRole('button', { name: fr.gm.returnToLobby.confirm }).click();

    await expect.poll(() => hub.returned).toEqual([snapshot.gameId]);
    await expect(page.getByRole('button', { name: fr.gm.start.action })).toBeVisible();
    await expect(page.getByRole('button', { name: fr.gm.returnToLobby.action })).toHaveCount(0);
});

test('/gm/ does not offer to skip a round that failed only twice', async ({ page }) => {
    await serveGameMasterSnapshot(page, fakeRound(answeringView([])), failures(2));

    await openConsole(page);

    await expect(page.getByRole('button', { name: fr.modes.quiz.gm.skipQuestion })).toBeVisible();
    await expect(page.getByText(fr.gm.skipRound.problem)).toHaveCount(0);
});

test('/gm/ warns that skipping the last question ends the round', async ({ page }) => {
    const view = answeringView([]);
    await serveGameMasterSnapshot(
        page,
        fakeRound({ ...view, questionNumber: 5, phase: 'Locked', answersCloseAt: null }),
    );

    await openConsole(page);
    await page.getByRole('button', { name: fr.modes.quiz.gm.skipQuestion }).click();

    const dialog = page.getByRole('dialog', {
        name: fill(fr.modes.quiz.gm.skipConfirm.title, { number: 5 }),
    });
    await expect(dialog.getByText(fr.modes.quiz.gm.skipConfirm.lastMessage)).toBeVisible();
});

test('/gm/ ranks the players between two rounds, then offers to start the next one by its title', async ({
    page,
}) => {
    await serveGameMasterSnapshot(page, {
        ...fakeLobby(),
        phase: 'BetweenRounds',
        players: [
            { id: zoe, nickname: 'Zoé', isConnected: true, score: 1000, reconnectionCode: null },
            { id: max, nickname: 'Max', isConnected: false, score: 2350, reconnectionCode: null },
        ],
        packCatalog: null,
        selectedPackId: 'soiree',
        packTitle: 'Grande soirée',
        round: firstRound,
        ranking: [
            {
                id: max,
                nickname: 'Max',
                isConnected: false,
                rank: 1,
                isTied: false,
                score: 2350,
                previousRank: null,
            },
            {
                id: zoe,
                nickname: 'Zoé',
                isConnected: true,
                rank: 2,
                isTied: false,
                score: 1000,
                previousRank: null,
            },
        ],
        nextRoundTitle: 'Culture générale',
    });

    await openConsole(page);

    await expect(page.getByText(roundText(fr.game.roundEnded, firstRound))).toBeVisible();
    await expect(
        page.getByRole('heading', { name: fill(fr.game.rankingAfter, { number: 1 }) }),
    ).toBeVisible();
    await expect(
        page.getByRole('list', { name: fr.game.rankingLabel }).getByRole('listitem'),
    ).toHaveText([
        `${rankText(fr.game.rank, 1)} Max ${countText(fr.game.points, 2350)}`,
        `${rankText(fr.game.rank, 2)} Zoé ${countText(fr.game.points, 1000)}`,
    ]);
    await expect(
        page.getByText(
            fill(fr.gm.nextRound.upcoming, { number: 2, count: 3, title: 'Culture générale' }),
        ),
    ).toBeVisible();
    await expect(page.getByRole('button', { name: fr.gm.nextRound.action })).toBeEnabled();
});

test('/gm/ shows the final ranking once the game is finished, with nothing left to start', async ({
    page,
}) => {
    const lastRound: RoundInfo = { ...firstRound, number: 3, title: 'Finale' };
    await serveGameMasterSnapshot(page, {
        ...fakeLobby(),
        phase: 'Finished',
        players: [
            { id: zoe, nickname: 'Zoé', isConnected: true, score: 3000, reconnectionCode: null },
            { id: max, nickname: 'Max', isConnected: false, score: 3000, reconnectionCode: null },
        ],
        packCatalog: null,
        selectedPackId: 'soiree',
        packTitle: 'Grande soirée',
        round: lastRound,
        ranking: [
            {
                id: max,
                nickname: 'Max',
                isConnected: false,
                rank: 1,
                isTied: true,
                score: 3000,
                previousRank: null,
            },
            {
                id: zoe,
                nickname: 'Zoé',
                isConnected: true,
                rank: 1,
                isTied: true,
                score: 3000,
                previousRank: null,
            },
        ],
    });

    await openConsole(page);

    await expect(page.getByRole('heading', { name: fr.game.finished })).toBeVisible();
    await expect(page.getByRole('heading', { name: fr.game.finalRanking })).toBeVisible();
    await expect(
        page.getByRole('list', { name: fr.game.rankingLabel }).getByRole('listitem'),
    ).toHaveText([
        `${rankText(fr.game.rank, 1)} Max ${countText(fr.game.points, 3000)}`,
        `${rankText(fr.game.rank, 1)} Zoé ${countText(fr.game.points, 3000)}`,
    ]);
    // The game is over: neither a round to play nor a game to start, but a new game from the lobby.
    for (const action of [fr.gm.nextRound.action, fr.gm.start.action]) {
        await expect(page.getByRole('button', { name: action })).toHaveCount(0);
    }
    await expect(page.getByRole('button', { name: fr.gm.returnToLobby.action })).toBeEnabled();
});

test('/gm/ goes back to the lobby at once once the game is finished', async ({ page }) => {
    const snapshot: GameMasterSnapshot = {
        ...fakeLobby(),
        phase: 'Finished',
        packCatalog: null,
        selectedPackId: 'soiree',
        packTitle: 'Grande soirée',
        round: { ...firstRound, number: 3, title: 'Finale' },
    };
    const hub = await serveGameMasterSnapshot(page, snapshot);

    await openConsole(page);
    await page.getByRole('button', { name: fr.gm.returnToLobby.action }).click();

    // Nothing is lost once finished: no confirmation.
    await expect.poll(() => hub.returned).toEqual([snapshot.gameId]);
    await expect(page.getByRole('dialog')).toHaveCount(0);
    await expect(page.getByRole('button', { name: fr.gm.start.action })).toBeVisible();
});

/** The console of a restarted server that found `savedGame`. */
function fakeResumePending(savedGame: Partial<GameMasterSavedGame> = {}): GameMasterSnapshot {
    return {
        ...fakeLobby(),
        phase: 'ResumePending',
        packCatalog: null,
        savedGame: {
            gameId: '0b7c4a5e-5d3e-4b8a-9c3f-2a1d6e8f9b70' as GameId,
            savedAt: Date.UTC(2026, 9, 4, 21, 4, 5),
            phase: 'Round',
            packTitle: 'Grande soirée',
            round: { ...firstRound, number: 2, count: 3, title: 'Finale' },
            step: { number: 4, count: 10 },
            playerCount: 3,
            missingMedia: [],
            ...savedGame,
        },
    };
}

test('/gm/ offers to resume the game found, describing where it stopped', async ({ page }) => {
    const hub = await serveGameMasterSnapshot(page, fakeResumePending());

    await openConsole(page);

    await expect(page.getByRole('heading', { name: fr.gm.resume.title })).toBeVisible();
    await expect(page.getByText('Grande soirée')).toBeVisible();
    await expect(page.getByText('Manche 2/3 · Finale · Question 4/10')).toBeVisible();
    await expect(page.getByText(countText(fr.gm.resume.players, 3))).toBeVisible();
    // Nothing of the usual console while the game master decides.
    await expect(page.getByRole('button', { name: fr.gm.start.action })).toHaveCount(0);
    await page.getByRole('button', { name: fr.gm.resume.resume }).click();
    await expect
        .poll(() => hub.resolved)
        .toEqual([{ savedGameId: '0b7c4a5e-5d3e-4b8a-9c3f-2a1d6e8f9b70', resume: true }]);
});

test('/gm/ starts a new game instead only once confirmed', async ({ page }) => {
    const hub = await serveGameMasterSnapshot(page, fakeResumePending());
    await openConsole(page);

    await page.getByRole('button', { name: fr.gm.resume.newGame }).click();
    const dialog = page.getByRole('dialog', { name: fr.gm.resume.confirmTitle });
    await expect(dialog).toBeVisible();
    await dialog.getByRole('button', { name: fr.gm.resume.cancel }).click();
    expect(hub.resolved).toEqual([]);
    await page.getByRole('button', { name: fr.gm.resume.newGame }).click();
    await page
        .getByRole('dialog', { name: fr.gm.resume.confirmTitle })
        .getByRole('button', { name: fr.gm.resume.confirm })
        .click();

    await expect
        .poll(() => hub.resolved)
        .toEqual([{ savedGameId: '0b7c4a5e-5d3e-4b8a-9c3f-2a1d6e8f9b70', resume: false }]);
});

test('/gm/ cannot resume while media files are missing, until they are checked again', async ({
    page,
}) => {
    await serveGameMasterSnapshot(
        page,
        fakeResumePending({ missingMedia: ['images/drapeau.png', 'images/tour.jpg'] }),
    );
    await openConsole(page);

    const resume = page.getByRole('button', { name: fr.gm.resume.resume });
    await expect(resume).toBeDisabled();
    await expect(page.getByText(countText(fr.gm.resume.missingMedia, 2))).toBeVisible();
    await expect(
        page.getByRole('list', { name: fr.gm.resume.missingMediaLabel }).getByRole('listitem'),
    ).toHaveText(['images/drapeau.png', 'images/tour.jpg']);
    await page.getByRole('button', { name: fr.gm.resume.checkAgain }).click();

    await expect(resume).toBeEnabled();
    await expect(page.getByRole('list', { name: fr.gm.resume.missingMediaLabel })).toHaveCount(0);
});
