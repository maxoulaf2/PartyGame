import { expect, test, type Page } from '@playwright/test';
import { gameMasterCodeKey } from '../src/shared/connection/codeStorage.ts';
import type {
    GameId,
    GameMasterRoundView,
    GameMasterSnapshot,
    PlayerId,
    QuizGameMasterView,
    RoundId,
    RoundInfo,
} from '../src/shared/contracts';
import { countText } from '../src/shared/i18n/countText.ts';
import { fill, roundText } from '../src/shared/i18n/fill.ts';
import { fr } from '../src/shared/i18n/fr.ts';
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
    };
}

const firstRound: RoundInfo = {
    roundId: '0f8fad5b-d9cb-469f-a165-70867728950e' as RoundId,
    number: 1,
    count: 3,
    title: 'Échauffement',
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
        choices: [
            { letter: 'A', text: 'Sydney', correct: false, answerCount: 0 },
            { letter: 'B', text: 'Canberra', correct: true, answerCount: 0 },
            { letter: 'C', text: 'Melbourne', correct: false, answerCount: 0 },
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
        'A Sydney',
        `B Canberra ${fr.modes.quiz.correct}`,
        'C Melbourne',
    ]);
    await expect(page.getByRole('button', { name: fr.modes.quiz.gm.openAnswers })).toBeEnabled();
    await expect(page.getByRole('button', { name: fr.modes.quiz.gm.skipQuestion })).toBeEnabled();
    // Nothing to count before the answers open.
    await expect(page.getByRole('list', { name: fr.modes.quiz.gm.answersLabel })).toHaveCount(0);
    await expect(page.getByRole('timer')).toHaveCount(0);
    await expect(page.getByRole('button', { name: fr.gm.start.action })).toHaveCount(0);
});

test('/gm/ waits neutrally on a round of a mode it does not know', async ({ page }) => {
    await serveGameMasterSnapshot(
        page,
        fakeRound({ type: 'blindtest' } as unknown as GameMasterRoundView),
    );

    await openConsole(page);

    await expect(page.getByText(fr.gm.inProgress, { exact: true })).toBeVisible();
    await expect(page.getByText('blindtest')).toHaveCount(0);
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
        choices: [
            { letter: 'A', text: 'Sydney', correct: false, answerCount: countOf('A') },
            { letter: 'B', text: 'Canberra', correct: true, answerCount: countOf('B') },
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
    await expect(page.getByText(fr.modes.quiz.gm.allAnswered)).toHaveCount(0);
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
    await expect(page.getByRole('button', { name: fr.modes.quiz.gm.lockAnswers })).toBeEnabled();
    await expect(page.getByRole('button', { name: fr.modes.quiz.gm.openAnswers })).toHaveCount(0);
});

test('/gm/ tells when every player taking part answered', async ({ page }) => {
    const view = answeringView([
        { playerId: zoe, nickname: 'Zoé', choice: 'B', points: null },
        { playerId: max, nickname: 'Max', choice: 'A', points: null },
    ]);
    await serveGameMasterSnapshot(page, fakeRound(view));

    await openConsole(page);

    await expect(page.getByText(fr.modes.quiz.gm.allAnswered)).toBeVisible();
    await expect(page.getByRole('button', { name: fr.modes.quiz.gm.lockAnswers })).toBeEnabled();
});

test('/gm/ offers to reveal the answer once the answers are locked', async ({ page }) => {
    const view = answeringView([{ playerId: zoe, nickname: 'Zoé', choice: 'B', points: null }]);
    await serveGameMasterSnapshot(
        page,
        fakeRound({ ...view, phase: 'Locked', answersCloseAt: null }),
    );

    await openConsole(page);

    await expect(page.getByText(fr.modes.quiz.timeUp)).toBeVisible();
    await expect(page.getByRole('button', { name: fr.modes.quiz.gm.revealAnswer })).toBeEnabled();
    await expect(page.getByRole('button', { name: fr.modes.quiz.gm.lockAnswers })).toHaveCount(0);
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
