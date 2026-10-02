import { describe, expect, it, vi } from 'vitest';
import type {
    GameId,
    IGameClient,
    JoinResult,
    PlayerId,
    PlayerSnapshot,
    ResumeSessionResult,
} from '../contracts';
import type { CodeStorage } from './codeStorage';
import type { GameConnection } from './gameHub';
import { PlayerSession } from './playerSession.svelte';
import { SnapshotStore } from './snapshotStore.svelte';

const gameId = '6f9619ff-8b86-d011-b42d-00cf4fc964ff' as GameId;
const playerId = '00000001-0000-0000-0000-000000000000' as PlayerId;
const token = 'q1w2e3r4t5y6u7i8o9p0aa';

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
    return { gameId, version, phase: 'Lobby', playerId, nickname, playerCount: 1 };
}

type Request = { nickname: string } | { token: string };

/**
 * A server that registers any nickname except those already taken, recognizes the tokens it
 * issued, and can become unreachable or restart.
 */
function fakeServer(options: { startFails?: boolean; taken?: string[]; known?: string[] } = {}) {
    let reachable = true;
    let version = 2;
    const known = new Set(options.known);
    const handlers = new Map<string, (snapshot: PlayerSnapshot) => void>();
    const callbacks = { reconnecting: () => {}, reconnected: () => {} };
    const connection = {
        start: vi.fn(() =>
            options.startFails ? Promise.reject(new Error('offline')) : Promise.resolve(),
        ),
        stop: vi.fn(() => Promise.resolve()),
        invoke: vi.fn(
            async (method: string, request: Request): Promise<JoinResult | ResumeSessionResult> => {
                if (!reachable) {
                    throw new Error('disconnected');
                }
                if ('token' in request) {
                    if (!known.has(request.token)) {
                        return { refusal: 'SessionUnknown', playerId: null };
                    }
                    // Like the hub: the current snapshot reaches the phone before the answer.
                    handlers.get('ReceivePlayerSnapshot')?.(snapshot(version));
                    return { refusal: null, playerId };
                }
                if (options.taken?.includes(request.nickname)) {
                    return { refusal: 'NicknameTaken', playerId: null, token: null };
                }
                known.add(token);
                version++;
                handlers.get('ReceivePlayerSnapshot')?.(snapshot(version, request.nickname));
                return { refusal: null, playerId, token };
            },
        ),
        on: vi.fn((message: string, handler: (snapshot: PlayerSnapshot) => void) => {
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
        send: (snapshot: PlayerSnapshot) => handlers.get('ReceivePlayerSnapshot')?.(snapshot),
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
) {
    const store = new SnapshotStore<PlayerSnapshot>();
    const session = new PlayerSession(store, tokens, nicknames, server.typed);
    session.start();
    await vi.waitFor(() => expect(session.connected).toBe(true));
    return { session, store, server, tokens, nicknames };
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
        const session = new PlayerSession(store, memoryStorage(), memoryStorage(), server.typed);
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
        const session = new PlayerSession(store, memoryStorage(), memoryStorage(), server.typed);
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
});
