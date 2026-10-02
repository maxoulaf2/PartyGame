import { describe, expect, it, vi } from 'vitest';
import type { DisplaySnapshot, GameId, IGameClient } from '../contracts';
import { connectDisplay } from './displayConnection';
import type { GameConnection } from './gameHub';
import { SnapshotStore } from './snapshotStore.svelte';

const gameId = '6f9619ff-8b86-d011-b42d-00cf4fc964ff' as GameId;

function fakeConnection(startFails = false) {
    const handlers = new Map<string, (snapshot: DisplaySnapshot) => void>();
    const unsubscribe = vi.fn();
    let reconnected = () => {};
    const connection = {
        start: vi.fn(() => (startFails ? Promise.reject(new Error('offline')) : Promise.resolve())),
        stop: vi.fn(() => Promise.resolve()),
        invoke: vi.fn(() => Promise.resolve({ refusal: null })),
        on: vi.fn((message: string, handler: (snapshot: DisplaySnapshot) => void) => {
            handlers.set(message, handler);
            return unsubscribe;
        }),
        onReconnecting: vi.fn(),
        onReconnected: vi.fn((callback: () => void) => {
            reconnected = callback;
        }),
        onClose: vi.fn(),
    };
    // The fake only implements what connectDisplay uses, with loose signatures.
    const typed = connection as unknown as GameConnection<IGameClient>;
    return { connection, typed, handlers, unsubscribe, reconnect: () => reconnected() };
}

describe('connectDisplay', () => {
    it('announces the page as the TV screen without any code once connected', async () => {
        const { connection, typed } = fakeConnection();

        connectDisplay(new SnapshotStore<DisplaySnapshot>(), typed);

        await vi.waitFor(() =>
            expect(connection.invoke).toHaveBeenCalledWith('Announce', {
                role: 'Display',
                gameMasterCode: null,
            }),
        );
    });

    it('announces the page again when the connection comes back', async () => {
        const { connection, typed, reconnect } = fakeConnection();

        connectDisplay(new SnapshotStore<DisplaySnapshot>(), typed);
        await vi.waitFor(() => expect(connection.invoke).toHaveBeenCalledOnce());
        reconnect();

        expect(connection.invoke).toHaveBeenCalledTimes(2);
        expect(connection.invoke).toHaveBeenLastCalledWith('Announce', {
            role: 'Display',
            gameMasterCode: null,
        });
    });

    it('hands the display snapshots to the store', () => {
        const { typed, handlers } = fakeConnection();
        const store = new SnapshotStore<DisplaySnapshot>();

        connectDisplay(store, typed);
        handlers.get('ReceiveDisplaySnapshot')?.({
            gameId,
            version: 2,
            phase: 'Lobby',
            playerCount: 3,
        });

        expect(store.current?.playerCount).toBe(3);
    });

    it('stays quiet when the server cannot be reached', async () => {
        const { connection, typed } = fakeConnection(true);

        connectDisplay(new SnapshotStore<DisplaySnapshot>(), typed);

        await vi.waitFor(() => expect(connection.start).toHaveBeenCalled());
        expect(connection.invoke).not.toHaveBeenCalled();
    });

    it('stops handling snapshots and disconnects when disposed', () => {
        const { connection, typed, unsubscribe } = fakeConnection();

        const disconnect = connectDisplay(new SnapshotStore<DisplaySnapshot>(), typed);
        disconnect();

        expect(unsubscribe).toHaveBeenCalledOnce();
        expect(connection.stop).toHaveBeenCalledOnce();
    });
});
