import { expect, test, type Page } from '@playwright/test';
import {
    gameMasterCodeKey,
    playerNicknameKey,
    playerTokenKey,
} from '../src/shared/connection/codeStorage.ts';
import type {
    ClientErrorReport,
    DisplayMediaFailureReport,
    DisplaySnapshot,
    GameId,
    GameMasterSnapshot,
    IncidentList,
    PlayerId,
    PlayerSnapshot,
    QuizDisplayView,
    QuizGameMasterView,
    QuizPlayerView,
    RoundId,
    RoundInfo,
} from '../src/shared/contracts';
import { countText } from '../src/shared/i18n/countText.ts';
import { fr } from '../src/shared/i18n/fr.ts';
import { serveScriptedHub, type ScriptedHub } from './fakeHub.ts';
import { gameMasterCode } from './gameServer.ts';

// Every view here renders a snapshot the server would never send: one the view cannot render,
// as a bug of its mode would leave it. The page must give way to its waiting screen without any
// technical message, tell the server, and come back by itself with the next snapshot.

const gameId = '6f9619ff-8b86-d011-b42d-00cf4fc964ff' as GameId;
const playerId = '00000001-0000-0000-0000-000000000000' as PlayerId;

const round: RoundInfo = {
    roundId: '0f8fad5b-d9cb-469f-a165-70867728950e' as RoundId,
    number: 1,
    count: 2,
    title: 'Échauffement',
    mode: 'quiz',
    description: null,
};

const question = 'Quelle est la capitale de l’Australie ?';

/**
 * Choices a view cannot render, as a bug of its mode would leave them: numbers rather than letters,
 * or no list at all.
 */
const broken = { choices: [1, 2] } as unknown as Record<string, never>;
const missing = { choices: null } as unknown as Record<string, never>;

function displayView(view: Partial<QuizDisplayView> = {}): QuizDisplayView {
    return {
        type: 'quiz',
        questionNumber: 1,
        questionCount: 3,
        phase: 'Presentation',
        text: question,
        imageUrl: null,
        choices: [
            { letter: 'A', text: 'Sydney' },
            { letter: 'B', text: 'Canberra' },
        ],
        choiceCount: 2,
        answersCloseAt: null,
        answeredCount: 0,
        participantCount: 0,
        reveal: null,
        ...view,
    };
}

function displaySnapshot(version: number, view: Partial<DisplaySnapshot> = {}): DisplaySnapshot {
    return {
        gameId,
        version,
        phase: 'Round',
        joinAddress: '192.168.1.42',
        players: [{ id: playerId, nickname: 'Zoé', isConnected: true }],
        packTitle: 'Grande soirée',
        round,
        roundView: displayView(),
        ranking: [],
        joinCodeShown: false,
        preview: null,
        finishedAt: null,
        pausedAt: null,
        ...view,
    };
}

function playerSnapshot(version: number, view: Partial<QuizPlayerView> = {}): PlayerSnapshot {
    return {
        gameId,
        version,
        phase: 'Round',
        playerId,
        nickname: 'Zoé',
        score: 0,
        playerCount: 1,
        round,
        roundView: {
            type: 'quiz',
            questionNumber: 1,
            questionCount: 3,
            phase: 'Answering',
            choices: ['A', 'B'],
            shownChoiceCount: 2,
            answersCloseAt: null,
            participating: true,
            answer: null,
            correctChoice: null,
            verdict: null,
            points: null,
            ...view,
        },
        standing: null,
        finishedAt: null,
        pausedAt: null,
    };
}

function gameMasterSnapshot(
    version: number,
    view: Partial<QuizGameMasterView> = {},
): GameMasterSnapshot {
    return {
        gameId,
        version,
        phase: 'Round',
        players: [
            {
                id: playerId,
                nickname: 'Zoé',
                isConnected: true,
                score: 0,
                reconnectionCode: 'ABC234',
            },
        ],
        minimumPlayerCount: 1,
        joinAddress: '192.168.1.42',
        joinAddressCandidates: [{ address: '192.168.1.42', interfaceName: 'Wi-Fi' }],
        packCatalog: null,
        selectedPackId: 'soiree',
        packTitle: 'Grande soirée',
        round,
        roundView: {
            type: 'quiz',
            questionNumber: 1,
            questionCount: 3,
            phase: 'Presentation',
            text: question,
            questionShown: true,
            choices: [
                { letter: 'A', text: 'Sydney', correct: false, shown: true, answerCount: 0 },
                { letter: 'B', text: 'Canberra', correct: true, shown: true, answerCount: 0 },
            ],
            answersCloseAt: null,
            answers: [],
            ...view,
        },
        ranking: [],
        nextRoundTitle: null,
        roundSkipped: false,
        savedGame: null,
        joinCodeShown: false,
        preview: null,
        pausedAt: null,
    };
}

/** The errors of rendering the page reported to the server. */
function renderFailures(hub: ScriptedHub): ClientErrorReport[] {
    return hub.invocations
        .filter((invocation) => invocation.target === 'ReportClientError')
        .map((invocation) => invocation.args[0] as ClientErrorReport)
        .filter((report) => report.kind === 'RenderFailed');
}

/** Whatever a technical message would show: the name or the message of the error. */
async function expectNoTechnicalMessage(page: Page): Promise<void> {
    await expect(page.getByText(/TypeError|null|undefined|Cannot read/)).toHaveCount(0);
}

test.describe('the TV screen', () => {
    test('shows its waiting screen instead of a view of a round that fails, then the view again', async ({
        page,
    }) => {
        const hub = await serveScriptedHub(
            page,
            'ReceiveDisplaySnapshot',
            displaySnapshot(1, { roundView: displayView(missing) }),
        );

        await page.goto('/display/');

        await expect(page.getByText(fr.display.continuing)).toBeVisible();
        await expect(page.getByRole('heading', { name: fr.app.name })).toBeVisible();
        await expectNoTechnicalMessage(page);
        await expect.poll(() => renderFailures(hub).length).toBe(1);
        expect(renderFailures(hub)[0]).toMatchObject({
            role: 'Display',
            page: '/display/',
            roundViewType: 'quiz',
            snapshotVersion: 1,
        });

        // A passing error: the next snapshot shows the view again.
        hub.push(displaySnapshot(2));

        await expect(page.getByRole('heading', { name: question })).toBeVisible();
        await expect(page.getByText(fr.display.continuing)).toHaveCount(0);
    });

    test('shows its waiting screen instead of a screen outside a round that fails', async ({
        page,
    }) => {
        // The final ranking, without its ranking.
        const hub = await serveScriptedHub(
            page,
            'ReceiveDisplaySnapshot',
            displaySnapshot(1, {
                phase: 'Finished',
                roundView: null,
                ranking: null as unknown as [],
            }),
        );

        await page.goto('/display/');

        await expect(page.getByText(fr.display.continuing)).toBeVisible();
        await expectNoTechnicalMessage(page);
        await expect.poll(() => renderFailures(hub).length).toBe(1);

        hub.push(displaySnapshot(2));

        await expect(page.getByRole('heading', { name: question })).toBeVisible();
    });

    test('shows the question without an image it cannot load, and tells the server', async ({
        page,
    }) => {
        await page.route('**/media/missing-image', (route) => route.fulfill({ status: 404 }));
        const hub = await serveScriptedHub(
            page,
            'ReceiveDisplaySnapshot',
            displaySnapshot(1, { roundView: displayView({ imageUrl: '/media/missing-image' }) }),
        );

        await page.goto('/display/');

        await expect(page.getByRole('heading', { name: question })).toBeVisible();
        await expect
            .poll(() =>
                hub.invocations
                    .filter((invocation) => invocation.target === 'ReportDisplayMediaFailure')
                    .map((invocation) => invocation.args[0] as DisplayMediaFailureReport),
            )
            .toEqual([{ mediaId: 'missing-image' }]);
        await expect(page.getByRole('img', { name: fr.modes.quiz.display.imageLabel })).toHaveCount(
            0,
        );
        await expect(page.getByText(fr.display.continuing)).toHaveCount(0);
    });
});

test('the phone shows its waiting screen, with nothing to tap, instead of a view that fails', async ({
    page,
}) => {
    await page.addInitScript(
        ([tokenKey, nicknameKey]) => {
            localStorage.setItem(tokenKey, 'token-zoe');
            localStorage.setItem(nicknameKey, 'Zoé');
        },
        [playerTokenKey, playerNicknameKey] as const,
    );
    const hub = await serveScriptedHub(
        page,
        'ReceivePlayerSnapshot',
        playerSnapshot(1, broken as Partial<QuizPlayerView>),
    );

    await page.goto('/');

    await expect(page.getByText(fr.player.inProgress)).toBeVisible();
    await expect(page.getByRole('button')).toHaveCount(0);
    await expect(page.getByRole('textbox')).toHaveCount(0);
    await expectNoTechnicalMessage(page);
    await expect.poll(() => renderFailures(hub).map((report) => report.role)).toEqual(['Player']);

    hub.push(playerSnapshot(2));

    await expect(page.getByRole('list', { name: fr.modes.quiz.choicesLabel })).toBeVisible();
    await expect(page.getByText(fr.player.inProgress)).toHaveCount(0);
});

test('the GM console keeps its other controls while the view of the round fails', async ({
    page,
}) => {
    await page.addInitScript(
        ([key, code]) => {
            localStorage.setItem(key, code);
        },
        [gameMasterCodeKey, gameMasterCode] as const,
    );
    // The TV screen could not show the round either: skipping it is offered.
    const incidents: IncidentList = {
        version: 1,
        incidents: [
            {
                id: 1,
                code: 'DisplayViewFailed',
                round,
                role: null,
                step: null,
                count: 1,
                lastOccurredAt: 1_790_000_000_000,
            },
        ],
        failingRounds: [round.roundId],
    };
    const hub = await serveScriptedHub(
        page,
        'ReceiveGameMasterSnapshot',
        gameMasterSnapshot(1, missing as Partial<QuizGameMasterView>),
        [{ target: 'ReceiveIncidents', arguments: [incidents] }],
    );

    await page.goto('/gm/');

    await expect(page.getByText(fr.gm.roundViewUnavailable)).toBeVisible();
    await expect(page.getByRole('heading', { name: fr.gm.consoleTitle })).toBeVisible();
    await expect(page.getByRole('heading', { name: round.title })).toBeVisible();
    await expect(page.getByRole('list', { name: fr.gm.playerListLabel })).toContainText('Zoé');
    await expect(page.getByRole('button', { name: fr.gm.skipRound.action })).toBeEnabled();
    await page.getByRole('button', { name: countText(fr.gm.incidents.counter, 1) }).click();
    await expect(page.getByText(fr.gm.incidents.codes.DisplayViewFailed)).toBeVisible();
    await expectNoTechnicalMessage(page);
    await expect
        .poll(() => renderFailures(hub).map((report) => report.role))
        .toEqual(['GameMaster']);

    hub.push(gameMasterSnapshot(2));

    await expect(page.getByRole('heading', { name: question })).toBeVisible();
    await expect(page.getByText(fr.gm.roundViewUnavailable)).toHaveCount(0);
});
