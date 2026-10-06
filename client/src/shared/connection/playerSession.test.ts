import { describe, expect, it, vi } from 'vitest';
import type {
    GameId,
    IGameClient,
    JoinResult,
    PlayerId,
    PlayerSnapshot,
    RecoverSessionResult,
    ResumeSessionResult,
    RoundId,
} from '../contracts';
import type { CodeStorage } from './codeStorage';
import type { GameConnection } from './gameHub';
import { PlayerSession } from './playerSession.svelte';
import { SnapshotStore } from './snapshotStore.svelte';

const gameId = '6f9619ff-8b86-d011-b42d-00cf4fc964ff' as GameId;
const playerId = '00000001-0000-0000-0000-000000000000' as PlayerId;
const token = 'q1w2e3r4t5y6u7i8o9p0aa';
const roundId = '0f8fad5b-d9cb-469f-a165-70867728950e' as RoundId;

function memoryStorage(initial: string | null = null): CodeStorage & { value: string | null } {
    const storage = {
        value: initial,
        load: () => storage.value,
        save: (value: string) => {
            storage.value = value;
        },
        clear: () => {
            storage.value = null;
        },
    };
    return storage;
}

function snapshot(version: number, nickname = 'Zoé'): PlayerSnapshot {
    return {
        gameId,
        version,
        phase: 'Lobby',
        playerId,
        nickname,
        score: 0,
        playerCount: 1,
        round: null,
        roundView: null,
        standing: null,
    };
}

type Request = { nickname: string } | { token: string } | { code: string };

/** The reconnection code the fake server knows, of a player whose last intent was numbered 4. */
const reconnectionCode = 'ABC234';

/**
 * A server that registers any nickname except those already taken, recognizes the tokens it
 * issued, and can become unreachable or restart.
 */
function fakeServer(
    options: { startFails?: boolean; taken?: string[]; known?: string[]; pending?: boolean } = {},
) {
    let reachable = true;
    let pending = options.pending ?? false;
    const welcome = () =>
        handlers.get('ReceiveWelcome')?.({ buildId: null, gamePending: pending } as never);
    let version = 2;
    const known = new Set(options.known);
    // Loose: each message has its own payload.
    const handlers = new Map<string, (payload: never) => void>();
    const callbacks = { reconnecting: () => {}, reconnected: () => {} };
    const connection = {
        // Like the hub: every connection is welcomed before any answer.
        start: vi.fn(() => {
            if (options.startFails) {
                return Promise.reject(new Error('offline'));
            }
            welcome();
            return Promise.resolve();
        }),
        stop: vi.fn(() => Promise.resolve()),
        invoke: vi.fn(
            async (
                method: string,
                request: Request,
            ): Promise<JoinResult | ResumeSessionResult | RecoverSessionResult | null> => {
                if (!reachable) {
                    throw new Error('disconnected');
                }
                if (method === 'SendRoundIntent') {
                    return null;
                }
                if ('code' in request) {
                    if (request.code !== reconnectionCode) {
                        return { refusal: 'CodeUnknown', token: null, lastClientSeq: 0 };
                    }
                    known.add(token);
                    return { refusal: null, token, lastClientSeq: 4 };
                }
                if (pending) {
                    return 'token' in request
                        ? { refusal: 'GamePending', playerId: null }
                        : { refusal: 'GamePending', playerId: null, token: null };
                }
                if ('token' in request) {
                    if (!known.has(request.token)) {
                        return { refusal: 'SessionUnknown', playerId: null };
                    }
                    // Like the hub: the current snapshot reaches the phone before the answer.
                    handlers.get('ReceivePlayerSnapshot')?.(snapshot(version) as never);
                    return { refusal: null, playerId };
                }
                if (options.taken?.includes(request.nickname)) {
                    return { refusal: 'NicknameTaken', playerId: null, token: null };
                }
                known.add(token);
                version++;
                handlers.get('ReceivePlayerSnapshot')?.(
                    snapshot(version, request.nickname) as never,
                );
                return { refusal: null, playerId, token };
            },
        ),
        on: vi.fn((message: string, handler: (payload: never) => void) => {
            handlers.set(message, handler);
            return () => handlers.delete(message);
        }),
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
        send: (snapshot: PlayerSnapshot) =>
            handlers.get('ReceivePlayerSnapshot')?.(snapshot as never),
        /** The game master resumed the game found (`keepTokens`) or started a new one. */
        resolve: (keepTokens: boolean) => {
            pending = false;
            if (!keepTokens) {
                known.clear();
            }
            welcome();
        },
        drop: () => {
            reachable = false;
            callbacks.reconnecting();
        },
        restore: () => {
            reachable = true;
            callbacks.reconnected();
        },
        /** Forgets every token, as a restarted server does. */
        restart: () => {
            known.clear();
        },
    };
}

async function startedSession(
    server = fakeServer(),
    nicknames = memoryStorage(),
    tokens = memoryStorage(),
    intents = memoryStorage(),
) {
    const store = new SnapshotStore<PlayerSnapshot>();
    const session = new PlayerSession(store, tokens, nicknames, intents, server.typed);
    session.start();
    await vi.waitFor(() => expect(session.connected).toBe(true));
    return { session, store, server, tokens, nicknames, intents };
}

describe('PlayerSession', () => {
    it('waits for a nickname without sending anything', async () => {
        const { session, server } = await startedSession();

        expect(session.status).toBe('registering');
        expect(session.joined).toBe(false);
        expect(session.rememberedNickname).toBe('');
        expect(server.connection.invoke).not.toHaveBeenCalled();
    });

    it('joins, keeps the token and the nickname, and shows the snapshot of the player', async () => {
        const { session, store, server, tokens, nicknames } = await startedSession();

        const outcome = await session.join(' Zoé ');

        expect(outcome).toBe('joined');
        expect(server.connection.invoke).toHaveBeenCalledWith('JoinGame', { nickname: ' Zoé ' });
        expect(session.joined).toBe(true);
        expect(tokens.value).toBe(token);
        expect(nicknames.value).toBe('Zoé');
        expect(store.current?.nickname).toBe(' Zoé ');
    });

    it('answers the refusal of the server and remembers nothing', async () => {
        const { session, tokens, nicknames } = await startedSession(fakeServer({ taken: ['Zoé'] }));

        const outcome = await session.join('Zoé');

        expect(outcome).toBe('NicknameTaken');
        expect(session.joined).toBe(false);
        expect(tokens.value).toBeNull();
        expect(nicknames.value).toBeNull();
    });

    it('fills the form with the nickname it last joined with', async () => {
        const { session } = await startedSession(fakeServer(), memoryStorage('Zoé'));

        expect(session.rememberedNickname).toBe('Zoé');
    });

    it('sends nothing while the server cannot be reached', async () => {
        const store = new SnapshotStore<PlayerSnapshot>();
        const server = fakeServer({ startFails: true });
        const session = new PlayerSession(
            store,
            memoryStorage(),
            memoryStorage(),
            memoryStorage(),
            server.typed,
        );
        session.start();
        await Promise.resolve();

        expect(session.connected).toBe(false);
        expect(await session.join('Zoé')).toBe('unreachable');
        expect(server.connection.invoke).not.toHaveBeenCalled();
    });

    it('answers unreachable when the connection drops during the registration', async () => {
        const { session, server } = await startedSession();
        server.connection.invoke.mockRejectedValueOnce(new Error('disconnected'));

        expect(await session.join('Zoé')).toBe('unreachable');
        expect(session.joined).toBe(false);
    });

    it('refuses a registration answered without a token', async () => {
        const { session, server, tokens } = await startedSession();
        server.connection.invoke.mockResolvedValueOnce({ refusal: null, playerId, token: null });

        expect(await session.join('Zoé')).toBe('JoinFailed');
        expect(session.joined).toBe(false);
        expect(tokens.value).toBeNull();
    });

    it('does not join twice', async () => {
        const { session, server } = await startedSession();
        await session.join('Zoé');

        expect(await session.join('Max')).toBe('unreachable');
        expect(server.connection.invoke).toHaveBeenCalledTimes(1);
    });

    it('marks the snapshot stale while disconnected', async () => {
        const { session, store, server } = await startedSession();
        await session.join('Zoé');

        server.drop();

        expect(session.connected).toBe(false);
        expect(store.fresh).toBe(false);
        server.restore();
        expect(session.connected).toBe(true);
    });

    it('is synchronized on the form as soon as connected, without any snapshot', async () => {
        const store = new SnapshotStore<PlayerSnapshot>();
        const server = fakeServer();
        const session = new PlayerSession(
            store,
            memoryStorage(),
            memoryStorage(),
            memoryStorage(),
            server.typed,
        );
        expect(session.synchronized).toBe(false);

        session.start();

        await vi.waitFor(() => expect(session.synchronized).toBe(true));
        expect(store.current).toBeNull();
    });

    it('is synchronized again only once a fresh snapshot follows a lost connection', async () => {
        const { session, server } = await startedSession();
        await session.join('Zoé');
        expect(session.synchronized).toBe(true);

        server.connection.invoke.mockImplementationOnce(() => new Promise(() => {}));
        server.drop();
        expect(session.synchronized).toBe(false);
        server.restore();
        // Connected, but the resumption has not brought its snapshot yet.
        expect(session.connected).toBe(true);
        expect(session.synchronized).toBe(false);

        server.send(snapshot(4));
        expect(session.synchronized).toBe(true);
    });

    it('remembers the nickname the game master renamed the player to', async () => {
        const { session, store, server, nicknames } = await startedSession();
        await session.join('Zoé');

        server.send(snapshot(4, 'Léa'));

        expect(store.current?.nickname).toBe('Léa');
        expect(nicknames.value).toBe('Léa');
    });

    describe('with a kept token', () => {
        it('resumes the session at startup without showing the form', async () => {
            const server = fakeServer({ known: [token] });
            const store = new SnapshotStore<PlayerSnapshot>();
            const session = new PlayerSession(
                store,
                memoryStorage(token),
                memoryStorage('Zoé'),
                memoryStorage(),
                server.typed,
            );

            expect(session.status).toBe('resuming');
            session.start();

            await vi.waitFor(() => expect(session.status).toBe('joined'));
            expect(server.connection.invoke).toHaveBeenCalledWith('ResumeSession', { token });
            expect(server.connection.invoke).not.toHaveBeenCalledWith(
                'JoinGame',
                expect.anything(),
            );
            expect(store.current?.nickname).toBe('Zoé');
            expect(store.fresh).toBe(true);
        });

        it('presents the token again on every new connection', async () => {
            const { session, store, server } = await startedSession();
            await session.join('Zoé');

            server.drop();
            server.restore();

            await vi.waitFor(() =>
                expect(server.connection.invoke).toHaveBeenLastCalledWith('ResumeSession', {
                    token,
                }),
            );
            expect(session.joined).toBe(true);
            await vi.waitFor(() => expect(store.fresh).toBe(true));
        });

        it('forgets an unknown token and shows the form with the last nickname', async () => {
            const { session, server, tokens, nicknames } = await startedSession();
            await session.join('Zoé');

            server.drop();
            server.restart();
            server.restore();

            await vi.waitFor(() => expect(session.status).toBe('registering'));
            expect(tokens.value).toBeNull();
            expect(nicknames.value).toBe('Zoé');
            expect(session.rememberedNickname).toBe('Zoé');
            expect(await session.join('Zoé')).toBe('joined');
        });

        it('keeps the token when the connection drops during the resumption', async () => {
            const server = fakeServer({ known: [token] });
            server.connection.invoke.mockRejectedValueOnce(new Error('disconnected'));
            const { session, tokens } = await startedSession(
                server,
                memoryStorage('Zoé'),
                memoryStorage(token),
            );
            await vi.waitFor(() => expect(server.connection.invoke).toHaveBeenCalledOnce());

            expect(session.status).toBe('resuming');
            expect(tokens.value).toBe(token);

            server.drop();
            server.restore();
            await vi.waitFor(() => expect(session.status).toBe('joined'));
        });

        it('keeps the token when the server fails to resume the session', async () => {
            const server = fakeServer({ known: [token] });
            server.connection.invoke.mockResolvedValueOnce({
                refusal: 'ResumeFailed',
                playerId: null,
            });
            const { session, tokens } = await startedSession(
                server,
                memoryStorage('Zoé'),
                memoryStorage(token),
            );
            await vi.waitFor(() => expect(server.connection.invoke).toHaveBeenCalledOnce());

            expect(session.status).toBe('resuming');
            expect(tokens.value).toBe(token);
        });
    });

    describe('recover', () => {
        it('keeps the token the code gives, resumes the session, and numbers the next intents after the last one handled', async () => {
            const { session, server, tokens } = await startedSession();

            const outcome = await session.recover(reconnectionCode);

            expect(outcome).toBe('recognized');
            expect(tokens.value).toBe(token);
            await vi.waitFor(() => expect(session.status).toBe('joined'));
            expect(server.connection.invoke).toHaveBeenCalledWith('ResumeSession', { token });
            session.sendRoundIntent({
                type: 'quiz.submitAnswer',
                roundId,
                questionNumber: 1,
                choice: 'A',
            });
            await vi.waitFor(() => expect(session.pendingIntents).toEqual([]));
            expect(server.connection.invoke).toHaveBeenLastCalledWith(
                'SendRoundIntent',
                expect.objectContaining({ clientSeq: 5 }),
            );
        });

        it('answers the refusal of an unknown code and stays on the form', async () => {
            const { session, tokens } = await startedSession();

            expect(await session.recover('ZZZZZZ')).toBe('CodeUnknown');
            expect(session.status).toBe('registering');
            expect(tokens.value).toBeNull();
        });

        it('answers unreachable when the connection drops meanwhile', async () => {
            const { session, server } = await startedSession();
            server.connection.invoke.mockRejectedValueOnce(new Error('disconnected'));

            expect(await session.recover(reconnectionCode)).toBe('unreachable');
            expect(session.status).toBe('registering');
        });
    });

    describe('sendRoundIntent', () => {
        const answer = {
            type: 'quiz.submitAnswer',
            roundId,
            questionNumber: 1,
            choice: 'A',
        } as const;
        const second = { ...answer, questionNumber: 2, choice: 'B' } as const;

        /** The envelopes the server received, in order. */
        function sent(server: ReturnType<typeof fakeServer>): unknown[] {
            return server.connection.invoke.mock.calls
                .filter(([method]) => method === 'SendRoundIntent')
                .map(([, envelope]) => envelope);
        }

        it('sends each intent in its envelope, numbered from 1, once joined', async () => {
            const { session, server } = await startedSession();
            await session.join('Zoé');

            session.sendRoundIntent(answer);
            session.sendRoundIntent(second);

            await vi.waitFor(() => expect(session.pendingIntents).toEqual([]));
            expect(sent(server)).toEqual([
                { clientSeq: 1, intent: answer },
                { clientSeq: 2, intent: second },
            ]);
        });

        it('shows the intent as pending until the server acknowledges it', async () => {
            const { session, server } = await startedSession();
            await session.join('Zoé');
            let acknowledge = () => {};
            server.connection.invoke.mockImplementationOnce(
                () =>
                    new Promise((resolve) => {
                        acknowledge = () => resolve(null);
                    }),
            );

            session.sendRoundIntent(answer);

            expect(session.pendingIntents).toEqual([answer]);
            acknowledge();
            await vi.waitFor(() => expect(session.pendingIntents).toEqual([]));
        });

        it('ignores an intent before the player registers', async () => {
            const { session, server } = await startedSession();

            session.sendRoundIntent(answer);

            expect(session.pendingIntents).toEqual([]);
            expect(sent(server)).toEqual([]);
        });

        it('keeps an intent while disconnected, and sends it once the player is identified again', async () => {
            const { session, server } = await startedSession();
            await session.join('Zoé');
            server.drop();

            session.sendRoundIntent(answer);
            expect(session.pendingIntents).toEqual([answer]);
            server.restore();

            await vi.waitFor(() => expect(session.pendingIntents).toEqual([]));
            const methods = server.connection.invoke.mock.calls.map(([method]) => method);
            expect(methods.slice(-2)).toEqual(['ResumeSession', 'SendRoundIntent']);
            expect(sent(server)).toEqual([{ clientSeq: 1, intent: answer }]);
        });

        it('sends again, with the same number, an intent whose acknowledgment was lost', async () => {
            const { session, server } = await startedSession();
            await session.join('Zoé');
            server.connection.invoke.mockRejectedValueOnce(new Error('disconnected'));

            session.sendRoundIntent(answer);
            await vi.waitFor(() => expect(sent(server)).toHaveLength(1));
            server.drop();
            session.sendRoundIntent(second);
            server.restore();

            await vi.waitFor(() => expect(session.pendingIntents).toEqual([]));
            expect(sent(server)).toEqual([
                { clientSeq: 1, intent: answer },
                { clientSeq: 1, intent: answer },
                { clientSeq: 2, intent: second },
            ]);
        });

        it('sends the intents a reloaded page kept, once the player is recognized', async () => {
            const intents = memoryStorage();
            const first = await startedSession(
                fakeServer(),
                memoryStorage(),
                memoryStorage(),
                intents,
            );
            await first.session.join('Zoé');
            first.server.drop();
            first.session.sendRoundIntent(answer);

            // The page reloads: a new session, with what the previous one kept.
            const server = fakeServer({ known: [token] });
            const session = new PlayerSession(
                new SnapshotStore<PlayerSnapshot>(),
                memoryStorage(token),
                memoryStorage('Zoé'),
                intents,
                server.typed,
            );
            expect(session.pendingIntents).toEqual([answer]);
            session.start();

            await vi.waitFor(() => expect(session.pendingIntents).toEqual([]));
            expect(sent(server)).toEqual([{ clientSeq: 1, intent: answer }]);
            session.sendRoundIntent(second);
            await vi.waitFor(() => expect(sent(server)).toHaveLength(2));
            expect(sent(server)[1]).toEqual({ clientSeq: 2, intent: second });
        });

        it('starts the numbers over with the token of a new registration', async () => {
            const { session, server, intents } = await startedSession();
            await session.join('Zoé');
            session.sendRoundIntent(answer);
            await vi.waitFor(() => expect(sent(server)).toHaveLength(1));
            server.drop();
            session.sendRoundIntent(second);

            server.restart();
            server.restore();
            await vi.waitFor(() => expect(session.status).toBe('registering'));
            expect(session.pendingIntents).toEqual([]);
            expect(intents.value).toBeNull();
            await session.join('Zoé');
            session.sendRoundIntent(second);

            await vi.waitFor(() => expect(sent(server)).toHaveLength(2));
            expect(sent(server)[1]).toEqual({ clientSeq: 1, intent: second });
        });
    });

    describe('while the restarted server waits for the game master', () => {
        it('keeps a phone without token off the form until the game master decides', async () => {
            const { session, server } = await startedSession(fakeServer({ pending: true }));

            expect(session.gamePending).toBe(true);
            expect(await session.join('Zoé')).toBe('unreachable');
            expect(server.connection.invoke).not.toHaveBeenCalled();

            server.resolve(false);

            expect(session.gamePending).toBe(false);
            expect(await session.join('Zoé')).toBe('joined');
        });

        it('keeps the token and presents it again once the game is resumed', async () => {
            const server = fakeServer({ pending: true, known: [token] });
            const { session, tokens } = await startedSession(
                server,
                memoryStorage('Zoé'),
                memoryStorage(token),
            );
            await vi.waitFor(() =>
                expect(server.connection.invoke).toHaveBeenCalledWith('ResumeSession', { token }),
            );

            expect(session.status).toBe('resuming');
            expect(tokens.value).toBe(token);

            server.resolve(true);

            await vi.waitFor(() => expect(session.joined).toBe(true));
        });

        it('registers again, nickname kept, once a new game starts instead', async () => {
            const server = fakeServer({ pending: true, known: [token] });
            const { session, tokens } = await startedSession(
                server,
                memoryStorage('Zoé'),
                memoryStorage(token),
            );
            await vi.waitFor(() =>
                expect(server.connection.invoke).toHaveBeenCalledWith('ResumeSession', { token }),
            );

            server.resolve(false);

            await vi.waitFor(() => expect(session.status).toBe('registering'));
            expect(tokens.value).toBeNull();
            expect(session.rememberedNickname).toBe('Zoé');
        });

        it('presents the token again at once when the decision came before the refusal', async () => {
            const server = fakeServer({ known: [token] });
            const { session } = await startedSession(
                server,
                memoryStorage('Zoé'),
                memoryStorage(token),
            );
            await vi.waitFor(() => expect(session.joined).toBe(true));
            server.drop();
            // The refusal was read while pending, the welcome that ends it overtook it.
            server.connection.invoke.mockResolvedValueOnce({
                refusal: 'GamePending',
                playerId: null,
            });

            server.restore();

            await vi.waitFor(() =>
                expect(
                    server.connection.invoke.mock.calls.filter(
                        ([method]) => method === 'ResumeSession',
                    ),
                ).toHaveLength(3),
            );
            expect(session.joined).toBe(true);
        });
    });
});
