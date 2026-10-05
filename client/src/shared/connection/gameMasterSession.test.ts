import { describe, expect, it, vi } from 'vitest';
import type {
    AnnouncementResult,
    ChooseAdvertisedAddressResult,
    GameId,
    GameMasterSnapshot,
    IGameClient,
    IncidentList,
    PlayerId,
    ReloadPacksResult,
    RenamePlayerResult,
    RoundId,
    SelectPackResult,
    StartGameResult,
} from '../contracts';
import type { CodeStorage } from './codeStorage';
import type { GameConnection } from './gameHub';
import { GameMasterSession, isCodeComplete } from './gameMasterSession.svelte';
import { SnapshotStore } from './snapshotStore.svelte';

const gameId = '6f9619ff-8b86-d011-b42d-00cf4fc964ff' as GameId;
const goodCode = '123456';
const playerId = '00000001-0000-0000-0000-000000000000' as PlayerId;
const roundId = '0f8fad5b-d9cb-469f-a165-70867728950e' as RoundId;

function memoryStorage(initial: string | null = null): CodeStorage & { value: string | null } {
    const storage = {
        value: initial,
        load: () => storage.value,
        save: (code: string) => {
            storage.value = code;
        },
        clear: () => {
            storage.value = null;
        },
    };
    return storage;
}

/**
 * A server that accepts one code only, and whose connection can drop and come back. A rename gets
 * `renameAnswer`, a start `startAnswer`, an address choice `addressAnswer`, a pack choice
 * `packAnswer`, a reload `reloadAnswer`.
 */
function fakeServer(options: { startFails?: boolean } = {}) {
    let code = goodCode;
    let renameAnswer: RenamePlayerResult | null = { refusal: null };
    let startAnswer: StartGameResult | null = { refusal: null };
    let addressAnswer: ChooseAdvertisedAddressResult | null = { refusal: null };
    let packAnswer: SelectPackResult | null = { refusal: null };
    let reloadAnswer: ReloadPacksResult | null = { refusal: null };
    let reachable = true;
    const handlers = new Map<string, (message: GameMasterSnapshot | IncidentList) => void>();
    const callbacks = { reconnecting: () => {}, reconnected: () => {} };
    const connection = {
        start: vi.fn(() =>
            options.startFails ? Promise.reject(new Error('offline')) : Promise.resolve(),
        ),
        stop: vi.fn(() => Promise.resolve()),
        invoke: vi.fn(
            async (
                method: string,
                announcement: { gameMasterCode: string | null },
            ): Promise<
                | AnnouncementResult
                | RenamePlayerResult
                | StartGameResult
                | ChooseAdvertisedAddressResult
                | SelectPackResult
                | ReloadPacksResult
                | null
            > => {
                if (!reachable) {
                    throw new Error('disconnected');
                }
                if (method === 'RenamePlayer') {
                    return renameAnswer;
                }
                if (method === 'StartGame') {
                    return startAnswer;
                }
                if (method === 'ChooseAdvertisedAddress') {
                    return addressAnswer;
                }
                if (method === 'SelectPack') {
                    return packAnswer;
                }
                if (method === 'ReloadPacks') {
                    return reloadAnswer;
                }
                if (
                    method === 'ResolveSavedGame' ||
                    method === 'CheckSavedGameMedia' ||
                    method === 'NextRound' ||
                    method === 'SkipRound' ||
                    method === 'ShowJoinCode' ||
                    method === 'SendGameMasterRoundIntent'
                ) {
                    return null;
                }
                return {
                    refusal: announcement.gameMasterCode === code ? null : 'GameMasterCodeInvalid',
                };
            },
        ),
        on: vi.fn(
            (message: string, handler: (message: GameMasterSnapshot | IncidentList) => void) => {
                handlers.set(message, handler);
                return () => handlers.delete(message);
            },
        ),
        onReconnecting: vi.fn((callback: () => void) => {
            callbacks.reconnecting = callback;
        }),
        onReconnected: vi.fn((callback: () => void) => {
            callbacks.reconnected = callback;
        }),
    };
    // The fake only implements what the session uses, with loose signatures.
    const typed = connection as unknown as GameConnection<IGameClient>;
    return {
        connection,
        typed,
        send: (snapshot: GameMasterSnapshot) =>
            handlers.get('ReceiveGameMasterSnapshot')?.(snapshot),
        sendIncidents: (incidents: IncidentList) => handlers.get('ReceiveIncidents')?.(incidents),
        drop: () => {
            reachable = false;
            callbacks.reconnecting();
        },
        restore: () => {
            reachable = true;
            callbacks.reconnected();
        },
        restartWithCode: (newCode: string) => {
            code = newCode;
        },
        becomeUnreachable: () => {
            reachable = false;
        },
        answerRenamesWith: (answer: RenamePlayerResult | null) => {
            renameAnswer = answer;
        },
        answerStartsWith: (answer: StartGameResult | null) => {
            startAnswer = answer;
        },
        answerAddressChoicesWith: (answer: ChooseAdvertisedAddressResult | null) => {
            addressAnswer = answer;
        },
        answerPackChoicesWith: (answer: SelectPackResult | null) => {
            packAnswer = answer;
        },
        answerReloadsWith: (answer: ReloadPacksResult | null) => {
            reloadAnswer = answer;
        },
    };
}

function snapshot(version: number): GameMasterSnapshot {
    return {
        gameId,
        version,
        phase: 'Lobby',
        players: [],
        minimumPlayerCount: 1,
        joinAddress: '192.168.1.42',
        joinAddressCandidates: [{ address: '192.168.1.42', interfaceName: 'Wi-Fi' }],
        packCatalog: { directory: '/srv/packs', packs: [] },
        selectedPackId: null,
        packTitle: null,
        round: null,
        roundView: null,
        ranking: [],
        nextRoundTitle: null,
        roundSkipped: false,
        savedGame: null,
        joinCodeShown: false,
    };
}

async function startedSession(storage: CodeStorage, server = fakeServer()) {
    const store = new SnapshotStore<GameMasterSnapshot>();
    const session = new GameMasterSession(store, storage, server.typed);
    session.start();
    await vi.waitFor(() => expect(session.connected).toBe(true));
    return { session, store, server };
}

async function grantedSession() {
    const started = await startedSession(memoryStorage(goodCode));
    await vi.waitFor(() => expect(started.session.access).toBe('granted'));
    return started;
}

describe('isCodeComplete', () => {
    it.each(['123456', ' 123456 ', '000000'])('accepts %j', (code) => {
        expect(isCodeComplete(code)).toBe(true);
    });

    it.each(['', '12345', '1234567', '12 456', 'abcdef', '１２３４５６'])('refuses %j', (code) => {
        expect(isCodeComplete(code)).toBe(false);
    });
});

describe('GameMasterSession', () => {
    it('asks for the code when none is remembered, without announcing', async () => {
        const { session, server } = await startedSession(memoryStorage());

        expect(session.access).toBe('codeRequired');
        expect(session.problem).toBeNull();
        expect(server.connection.invoke).not.toHaveBeenCalled();
    });

    it('grants access and remembers the trimmed code once the server accepts it', async () => {
        const storage = memoryStorage();
        const { session, server } = await startedSession(storage);

        const outcome = await session.submit(' 123456 ');

        expect(outcome).toBe('granted');
        expect(session.access).toBe('granted');
        expect(storage.value).toBe(goodCode);
        expect(server.connection.invoke).toHaveBeenCalledWith('Announce', {
            role: 'GameMaster',
            gameMasterCode: goodCode,
        });
    });

    it('keeps the form while a typed code is being checked', async () => {
        const { session } = await startedSession(memoryStorage());

        const outcome = session.submit('654321');

        expect(session.access).toBe('codeRequired');
        await outcome;
    });

    it('reports a wrong code as invalid and remembers nothing', async () => {
        const storage = memoryStorage();
        const { session } = await startedSession(storage);

        const outcome = await session.submit('654321');

        expect(outcome).toBe('refused');
        expect(session.access).toBe('codeRequired');
        expect(session.problem).toBe('invalid');
        expect(storage.value).toBeNull();
    });

    it('clears the problem once the right code follows a wrong one', async () => {
        const { session } = await startedSession(memoryStorage());

        await session.submit('654321');
        await session.submit(goodCode);

        expect(session.access).toBe('granted');
        expect(session.problem).toBeNull();
    });

    it('presents the remembered code at connection, without showing the form', async () => {
        const server = fakeServer();
        const session = new GameMasterSession(
            new SnapshotStore<GameMasterSnapshot>(),
            memoryStorage(goodCode),
            server.typed,
        );

        expect(session.access).toBe('checking');
        session.start();

        await vi.waitFor(() => expect(session.access).toBe('granted'));
        expect(server.connection.invoke).toHaveBeenCalledOnce();
    });

    it('asks for the code again when the remembered one has expired, and forgets it', async () => {
        const storage = memoryStorage('111111');
        const { session } = await startedSession(storage);

        await vi.waitFor(() => expect(session.access).toBe('codeRequired'));
        expect(session.problem).toBe('expired');
        expect(storage.value).toBeNull();
    });

    it('presents the code again when the connection comes back, keeping the console', async () => {
        const { session, store, server } = await startedSession(memoryStorage(goodCode));
        await vi.waitFor(() => expect(session.access).toBe('granted'));
        server.send(snapshot(1));

        server.drop();
        expect(session.connected).toBe(false);
        expect(store.fresh).toBe(false);
        server.restore();

        expect(session.access).toBe('granted');
        expect(session.connected).toBe(true);
        await vi.waitFor(() => expect(server.connection.invoke).toHaveBeenCalledTimes(2));
        expect(session.access).toBe('granted');
    });

    it('is synchronized on the code form as soon as connected', async () => {
        const { session } = await startedSession(memoryStorage());

        expect(session.access).toBe('codeRequired');
        expect(session.synchronized).toBe(true);
    });

    it('is synchronized on the console only with a fresh snapshot', async () => {
        const { session, server } = await grantedSession();
        expect(session.synchronized).toBe(false);
        server.send(snapshot(1));
        expect(session.synchronized).toBe(true);

        server.drop();
        expect(session.synchronized).toBe(false);
        server.restore();
        await vi.waitFor(() => expect(server.connection.invoke).toHaveBeenCalledTimes(2));
        // Connected and granted again, but still showing the snapshot from before the outage.
        expect(session.synchronized).toBe(false);

        server.send(snapshot(2));
        expect(session.synchronized).toBe(true);
    });

    it('asks for the code when the server restarted with a new one meanwhile', async () => {
        const storage = memoryStorage(goodCode);
        const { session, server } = await startedSession(storage);
        await vi.waitFor(() => expect(session.access).toBe('granted'));

        server.drop();
        server.restartWithCode('999999');
        server.restore();

        await vi.waitFor(() => expect(session.access).toBe('codeRequired'));
        expect(session.problem).toBe('expired');
        expect(storage.value).toBeNull();
    });

    it('sends nothing while the server cannot be reached', async () => {
        const server = fakeServer({ startFails: true });
        const session = new GameMasterSession(
            new SnapshotStore<GameMasterSnapshot>(),
            memoryStorage(),
            server.typed,
        );

        session.start();
        await vi.waitFor(() => expect(server.connection.start).toHaveBeenCalled());
        const outcome = await session.submit(goodCode);

        expect(outcome).toBe('unreachable');
        expect(session.connected).toBe(false);
        expect(server.connection.invoke).not.toHaveBeenCalled();
    });

    it('sends nothing until the code has 6 digits', async () => {
        const { session, server } = await startedSession(memoryStorage());

        const outcome = await session.submit('12345');

        expect(outcome).toBe('unreachable');
        expect(server.connection.invoke).not.toHaveBeenCalled();
    });

    it('lets the code be typed again when the connection drops during the check', async () => {
        const { session, server } = await startedSession(memoryStorage());

        server.becomeUnreachable();
        const outcome = await session.submit(goodCode);

        expect(outcome).toBe('unreachable');
        expect(session.access).toBe('codeRequired');
        expect(session.problem).toBeNull();
    });

    it('hands the game master snapshots to the store', async () => {
        const { store, server } = await startedSession(memoryStorage(goodCode));

        server.send(snapshot(4));

        expect(store.current?.version).toBe(4);
    });

    it('hands the incidents to its inbox, which takes any list once the connection comes back', async () => {
        const { session, server } = await grantedSession();
        const incident = {
            id: 1,
            code: 'RoundHandlerFailed',
            round: null,
            role: null,
            step: null,
            count: 1,
            lastOccurredAt: 1_790_000_000_000,
        } as const;

        server.sendIncidents({
            version: 3,
            incidents: [{ ...incident, count: 3 }],
            failingRounds: [],
        });
        server.drop();
        server.restore();
        // The server restarted meanwhile: it counts its incidents from 0 again.
        server.sendIncidents({ version: 1, incidents: [incident], failingRounds: [] });

        expect(session.incidents.incidents).toEqual([incident]);
        expect(session.incidents.unread).toBe(1);
    });

    it('stops handling snapshots and disconnects when disposed', () => {
        const store = new SnapshotStore<GameMasterSnapshot>();
        const server = fakeServer();
        const session = new GameMasterSession(store, memoryStorage(), server.typed);

        const disconnect = session.start();
        disconnect();
        server.send(snapshot(1));

        expect(store.current).toBeNull();
        expect(server.connection.stop).toHaveBeenCalledOnce();
    });

    describe('rename', () => {
        it('sends the player and the nickname as typed, and reports the rename', async () => {
            const { session, server } = await grantedSession();

            const outcome = await session.rename(playerId, ' Zoé ');

            expect(outcome).toBe('renamed');
            expect(server.connection.invoke).toHaveBeenLastCalledWith('RenamePlayer', {
                playerId,
                nickname: ' Zoé ',
            });
        });

        it.each(['NicknameTaken', 'NicknameInvalid', 'PlayerUnknown'] as const)(
            'reports the refusal %s of the server',
            async (refusal) => {
                const { session, server } = await grantedSession();
                server.answerRenamesWith({ refusal });

                expect(await session.rename(playerId, 'Max')).toBe(refusal);
            },
        );

        it('reports an ignored rename as unreachable, the code being checked again', async () => {
            const { session, server } = await grantedSession();
            server.answerRenamesWith(null);

            expect(await session.rename(playerId, 'Max')).toBe('unreachable');
        });

        it('reports a rename lost with the connection as unreachable', async () => {
            const { session, server } = await grantedSession();
            server.becomeUnreachable();

            expect(await session.rename(playerId, 'Max')).toBe('unreachable');
        });

        it('sends nothing while disconnected or without access', async () => {
            const { session, server } = await startedSession(memoryStorage());

            expect(await session.rename(playerId, 'Max')).toBe('unreachable');
            await session.submit(goodCode);
            server.drop();
            expect(await session.rename(playerId, 'Max')).toBe('unreachable');

            expect(server.connection.invoke).not.toHaveBeenCalledWith(
                'RenamePlayer',
                expect.anything(),
            );
        });
    });

    describe('startGame', () => {
        it('sends the intent without any message, and reports the start', async () => {
            const { session, server } = await grantedSession();

            const outcome = await session.startGame();

            expect(outcome).toBe('started');
            expect(server.connection.invoke).toHaveBeenLastCalledWith('StartGame');
        });

        it.each(['NotEnoughPlayers', 'AlreadyStarted', 'StartFailed'] as const)(
            'reports the refusal %s of the server',
            async (refusal) => {
                const { session, server } = await grantedSession();
                server.answerStartsWith({ refusal });

                expect(await session.startGame()).toBe(refusal);
            },
        );

        it('reports an ignored start as unreachable, the code being checked again', async () => {
            const { session, server } = await grantedSession();
            server.answerStartsWith(null);

            expect(await session.startGame()).toBe('unreachable');
        });

        it('reports a start lost with the connection as unreachable', async () => {
            const { session, server } = await grantedSession();
            server.becomeUnreachable();

            expect(await session.startGame()).toBe('unreachable');
        });

        it('sends nothing while disconnected or without access', async () => {
            const { session, server } = await startedSession(memoryStorage());

            expect(await session.startGame()).toBe('unreachable');
            await session.submit(goodCode);
            server.drop();
            expect(await session.startGame()).toBe('unreachable');

            expect(server.connection.invoke).not.toHaveBeenCalledWith('StartGame');
        });
    });

    describe('chooseAddress', () => {
        it('sends the chosen address, and reports it advertised', async () => {
            const { session, server } = await grantedSession();

            const outcome = await session.chooseAddress('10.0.0.2');

            expect(outcome).toBe('chosen');
            expect(server.connection.invoke).toHaveBeenLastCalledWith('ChooseAdvertisedAddress', {
                address: '10.0.0.2',
            });
        });

        it.each(['AddressUnknown', 'ChoiceFailed', 'MessageInvalid'] as const)(
            'reports the refusal %s of the server',
            async (refusal) => {
                const { session, server } = await grantedSession();
                server.answerAddressChoicesWith({ refusal });

                expect(await session.chooseAddress('10.0.0.2')).toBe(refusal);
            },
        );

        it('reports an ignored choice as unreachable, the code being checked again', async () => {
            const { session, server } = await grantedSession();
            server.answerAddressChoicesWith(null);

            expect(await session.chooseAddress('10.0.0.2')).toBe('unreachable');
        });

        it('reports a choice lost with the connection as unreachable', async () => {
            const { session, server } = await grantedSession();
            server.becomeUnreachable();

            expect(await session.chooseAddress('10.0.0.2')).toBe('unreachable');
        });

        it('sends nothing while disconnected or without access', async () => {
            const { session, server } = await startedSession(memoryStorage());

            expect(await session.chooseAddress('10.0.0.2')).toBe('unreachable');
            await session.submit(goodCode);
            server.drop();
            expect(await session.chooseAddress('10.0.0.2')).toBe('unreachable');

            expect(server.connection.invoke).not.toHaveBeenCalledWith(
                'ChooseAdvertisedAddress',
                expect.anything(),
            );
        });
    });

    describe('selectPack', () => {
        it('sends the chosen pack, and reports it selected', async () => {
            const { session, server } = await grantedSession();

            const outcome = await session.selectPack('soiree');

            expect(outcome).toBe('selected');
            expect(server.connection.invoke).toHaveBeenLastCalledWith('SelectPack', {
                packId: 'soiree',
            });
        });

        it.each([
            'PackUnknown',
            'PackInvalid',
            'AlreadyStarted',
            'SelectionFailed',
            'MessageInvalid',
        ] as const)('reports the refusal %s of the server', async (refusal) => {
            const { session, server } = await grantedSession();
            server.answerPackChoicesWith({ refusal });

            expect(await session.selectPack('soiree')).toBe(refusal);
        });

        it('reports an ignored or lost choice as unreachable', async () => {
            const { session, server } = await grantedSession();
            server.answerPackChoicesWith(null);
            expect(await session.selectPack('soiree')).toBe('unreachable');

            server.becomeUnreachable();
            expect(await session.selectPack('soiree')).toBe('unreachable');
        });

        it('sends nothing while disconnected or without access', async () => {
            const { session, server } = await startedSession(memoryStorage());

            expect(await session.selectPack('soiree')).toBe('unreachable');
            await session.submit(goodCode);
            server.drop();
            expect(await session.selectPack('soiree')).toBe('unreachable');

            expect(server.connection.invoke).not.toHaveBeenCalledWith(
                'SelectPack',
                expect.anything(),
            );
        });
    });

    describe('reloadPacks', () => {
        it('sends the intent without any message, and reports the reload', async () => {
            const { session, server } = await grantedSession();

            const outcome = await session.reloadPacks();

            expect(outcome).toBe('reloaded');
            expect(server.connection.invoke).toHaveBeenLastCalledWith('ReloadPacks');
        });

        it.each(['AlreadyStarted', 'ReloadFailed'] as const)(
            'reports the refusal %s of the server',
            async (refusal) => {
                const { session, server } = await grantedSession();
                server.answerReloadsWith({ refusal });

                expect(await session.reloadPacks()).toBe(refusal);
            },
        );

        it('reports an ignored or lost reload as unreachable', async () => {
            const { session, server } = await grantedSession();
            server.answerReloadsWith(null);
            expect(await session.reloadPacks()).toBe('unreachable');

            server.becomeUnreachable();
            expect(await session.reloadPacks()).toBe('unreachable');
        });

        it('sends nothing while disconnected or without access', async () => {
            const { session, server } = await startedSession(memoryStorage());

            expect(await session.reloadPacks()).toBe('unreachable');
            await session.submit(goodCode);
            server.drop();
            expect(await session.reloadPacks()).toBe('unreachable');

            expect(server.connection.invoke).not.toHaveBeenCalledWith('ReloadPacks');
        });
    });

    describe('nextRound', () => {
        it('names the round that just finished', async () => {
            const { session, server } = await grantedSession();

            const outcome = await session.nextRound(roundId);

            expect(outcome).toBe('sent');
            expect(server.connection.invoke).toHaveBeenLastCalledWith('NextRound', {
                afterRound: roundId,
            });
        });

        it('reports a request lost with the connection as unreachable', async () => {
            const { session, server } = await grantedSession();
            server.becomeUnreachable();

            expect(await session.nextRound(roundId)).toBe('unreachable');
        });

        it('sends nothing while disconnected or without access', async () => {
            const { session, server } = await startedSession(memoryStorage());

            expect(await session.nextRound(roundId)).toBe('unreachable');
            await session.submit(goodCode);
            server.drop();
            expect(await session.nextRound(roundId)).toBe('unreachable');

            expect(server.connection.invoke).not.toHaveBeenCalledWith(
                'NextRound',
                expect.anything(),
            );
        });
    });

    describe('resolveSavedGame', () => {
        it('names the game found, resumed or not', async () => {
            const { session, server } = await grantedSession();

            expect(await session.resolveSavedGame(gameId, true)).toBe('sent');
            expect(server.connection.invoke).toHaveBeenLastCalledWith('ResolveSavedGame', {
                savedGameId: gameId,
                resume: true,
            });
            expect(await session.resolveSavedGame(gameId, false)).toBe('sent');
            expect(server.connection.invoke).toHaveBeenLastCalledWith('ResolveSavedGame', {
                savedGameId: gameId,
                resume: false,
            });
        });

        it('sends nothing without access', async () => {
            const { session, server } = await startedSession(memoryStorage());

            expect(await session.resolveSavedGame(gameId, true)).toBe('unreachable');
            expect(await session.checkSavedGameMedia()).toBe('unreachable');
            expect(server.connection.invoke).not.toHaveBeenCalled();
        });
    });

    describe('checkSavedGameMedia', () => {
        it('asks the server to check the media files again', async () => {
            const { session, server } = await grantedSession();

            expect(await session.checkSavedGameMedia()).toBe('sent');
            expect(server.connection.invoke).toHaveBeenLastCalledWith('CheckSavedGameMedia');
        });
    });

    describe('skipRound', () => {
        it('names the round in progress', async () => {
            const { session, server } = await grantedSession();

            const outcome = await session.skipRound(roundId);

            expect(outcome).toBe('sent');
            expect(server.connection.invoke).toHaveBeenLastCalledWith('SkipRound', { roundId });
        });

        it('reports a request lost with the connection as unreachable', async () => {
            const { session, server } = await grantedSession();
            server.becomeUnreachable();

            expect(await session.skipRound(roundId)).toBe('unreachable');
        });

        it('sends nothing while disconnected or without access', async () => {
            const { session, server } = await startedSession(memoryStorage());

            expect(await session.skipRound(roundId)).toBe('unreachable');
            await session.submit(goodCode);
            server.drop();
            expect(await session.skipRound(roundId)).toBe('unreachable');

            expect(server.connection.invoke).not.toHaveBeenCalledWith(
                'SkipRound',
                expect.anything(),
            );
        });
    });

    describe('showJoinCode', () => {
        it('names the outcome rather than toggling', async () => {
            const { session, server } = await grantedSession();

            expect(await session.showJoinCode(true)).toBe('sent');
            expect(server.connection.invoke).toHaveBeenLastCalledWith('ShowJoinCode', {
                shown: true,
            });
        });
    });

    describe('sendRoundIntent', () => {
        const reveal = { type: 'quiz.revealAnswer', roundId, questionNumber: 1 } as const;

        it('hands the intent to the server', async () => {
            const { session, server } = await grantedSession();

            const outcome = await session.sendRoundIntent(reveal);

            expect(outcome).toBe('sent');
            expect(server.connection.invoke).toHaveBeenLastCalledWith(
                'SendGameMasterRoundIntent',
                reveal,
            );
        });

        it('reports an intent lost with the connection as unreachable', async () => {
            const { session, server } = await grantedSession();
            server.becomeUnreachable();

            expect(await session.sendRoundIntent(reveal)).toBe('unreachable');
        });

        it('sends nothing while disconnected or without access', async () => {
            const { session, server } = await startedSession(memoryStorage());

            expect(await session.sendRoundIntent(reveal)).toBe('unreachable');
            await session.submit(goodCode);
            server.drop();
            expect(await session.sendRoundIntent(reveal)).toBe('unreachable');

            expect(server.connection.invoke).not.toHaveBeenCalledWith(
                'SendGameMasterRoundIntent',
                expect.anything(),
            );
        });
    });
});
