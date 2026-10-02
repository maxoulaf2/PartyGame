import { HubConnectionState } from '@microsoft/signalr';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import type { AnnouncementResult } from '../contracts';
import {
    createGameConnection,
    reconnectDelay,
    type HubTransport,
    type WakeSource,
} from './gameHub';

function fakeTransport(answer: unknown = null) {
    const callbacks: {
        reconnecting: (error?: Error) => void;
        reconnected: (connectionId?: string) => void;
        close: (error?: Error) => void;
    } = { reconnecting: () => {}, reconnected: () => {}, close: () => {} };
    let state = HubConnectionState.Disconnected;
    const mocks = {
        start: vi.fn(async () => {
            state = HubConnectionState.Connected;
        }),
        stop: vi.fn(async () => {
            const wasActive = state !== HubConnectionState.Disconnected;
            state = HubConnectionState.Disconnected;
            if (wasActive) {
                callbacks.close();
            }
        }),
        invoke: vi.fn<(method: string, ...args: unknown[]) => Promise<unknown>>(async () => answer),
        on: vi.fn(),
        off: vi.fn(),
        onreconnecting: vi.fn((callback: (error?: Error) => void) => {
            callbacks.reconnecting = callback;
        }),
        onreconnected: vi.fn((callback: (connectionId?: string) => void) => {
            callbacks.reconnected = callback;
        }),
        onclose: vi.fn((callback: (error?: Error) => void) => {
            callbacks.close = callback;
        }),
    };
    // SignalR types invoke as generic in its answer, which a mock cannot express.
    const transport: HubTransport = {
        ...mocks,
        invoke: mocks.invoke as unknown as HubTransport['invoke'],
        get state() {
            return state;
        },
    };
    return {
        transport,
        mocks,
        /** The automatic reconnection of SignalR begins. */
        loseConnection: () => {
            state = HubConnectionState.Reconnecting;
            callbacks.reconnecting(new Error('lost'));
        },
        /** The automatic reconnection of SignalR succeeds. */
        reconnect: () => {
            state = HubConnectionState.Connected;
            callbacks.reconnected('new-id');
        },
        /** The server closes the connection for good. */
        close: () => {
            state = HubConnectionState.Disconnected;
            callbacks.close(new Error('closed by the server'));
        },
        /** The next `count` starts fail, as with the server unreachable. */
        failStarts: (count: number) => {
            for (let i = 0; i < count; i++) {
                mocks.start.mockRejectedValueOnce(new Error('unreachable'));
            }
        },
    };
}

/** A wake source the test triggers, as a page coming back to the foreground. */
function fakeWake() {
    let callback: (() => void) | null = null;
    const source: WakeSource = (onWake) => {
        callback = onWake;
        return () => {
            callback = null;
        };
    };
    return { source, wake: () => callback?.(), listening: () => callback !== null };
}

interface TestMessages {
    Moved(position: number, label: string): void;
}

describe('reconnectDelay', () => {
    it('tries at once, then waits longer and longer up to 10 s, forever', () => {
        expect([0, 1, 2, 3, 4, 5, 100].map(reconnectDelay)).toEqual([
            0, 1_000, 2_000, 5_000, 10_000, 10_000, 10_000,
        ]);
    });
});

describe('createGameConnection', () => {
    beforeEach(() => {
        vi.useFakeTimers();
    });

    afterEach(() => {
        vi.useRealTimers();
    });

    it('starts and stops the underlying connection', async () => {
        const { transport, mocks } = fakeTransport();
        const connection = createGameConnection(transport, fakeWake().source);

        await connection.start();
        await connection.stop();

        expect(mocks.start).toHaveBeenCalledOnce();
        expect(mocks.stop).toHaveBeenCalledOnce();
    });

    it('invokes a hub method by its name and resolves to its answer', async () => {
        const answer: AnnouncementResult = { refusal: 'GameMasterCodeInvalid' };
        const { transport, mocks } = fakeTransport(answer);
        const connection = createGameConnection(transport, fakeWake().source);

        const result = await connection.invoke('Announce', {
            role: 'GameMaster',
            gameMasterCode: '123456',
        });

        expect(result).toEqual(answer);
        expect(mocks.invoke).toHaveBeenCalledWith('Announce', {
            role: 'GameMaster',
            gameMasterCode: '123456',
        });
    });

    it('hands the messages of the server to their handler until unsubscribed', () => {
        const { transport, mocks } = fakeTransport();
        const connection = createGameConnection<TestMessages>(transport, fakeWake().source);
        const handler = vi.fn();

        const unsubscribe = connection.on('Moved', handler);
        const [name, callback] = mocks.on.mock.calls[0] as unknown as [
            string,
            (...args: unknown[]) => void,
        ];
        callback(3, 'left');
        unsubscribe();

        expect(name).toBe('Moved');
        expect(handler).toHaveBeenCalledWith(3, 'left');
        expect(mocks.off).toHaveBeenCalledWith('Moved', callback);
    });

    it('reports when the connection is lost and restored by SignalR', async () => {
        const fake = fakeTransport();
        const connection = createGameConnection(fake.transport, fakeWake().source);
        const lost = vi.fn();
        const restored = vi.fn();
        connection.onReconnecting(lost);
        connection.onReconnected(restored);
        await connection.start();

        fake.loseConnection();
        fake.reconnect();

        expect(lost).toHaveBeenCalledOnce();
        expect(restored).toHaveBeenCalledOnce();
    });

    it('reports every established connection: the first, then each restored one', async () => {
        const fake = fakeTransport();
        const connection = createGameConnection(fake.transport, fakeWake().source);
        const connected = vi.fn();
        connection.onConnected(connected);

        await connection.start();
        expect(connected).toHaveBeenCalledOnce();
        fake.loseConnection();
        fake.reconnect();
        expect(connected).toHaveBeenCalledTimes(2);
        fake.failStarts(1);
        fake.close();
        await vi.advanceTimersByTimeAsync(1_000);

        expect(connected).toHaveBeenCalledTimes(3);
    });

    it('keeps trying to start until the server answers', async () => {
        const fake = fakeTransport();
        fake.failStarts(8);
        const connection = createGameConnection(fake.transport, fakeWake().source);
        const started = vi.fn();

        void connection.start().then(started);
        // Attempts at 0, 0, 1, 3, 8, 18, 28, 38 and 48 s: the ninth succeeds.
        await vi.advanceTimersByTimeAsync(47_999);
        expect(started).not.toHaveBeenCalled();
        await vi.advanceTimersByTimeAsync(1);

        expect(started).toHaveBeenCalledOnce();
        expect(fake.mocks.start).toHaveBeenCalledTimes(9);
    });

    it('starts over when the connection closes, however long the outage', async () => {
        const fake = fakeTransport();
        const connection = createGameConnection(fake.transport, fakeWake().source);
        const lost = vi.fn();
        const restored = vi.fn();
        connection.onReconnecting(lost);
        connection.onReconnected(restored);
        await connection.start();

        // A few minutes without network: every attempt fails.
        fake.failStarts(40);
        fake.close();
        await vi.advanceTimersByTimeAsync(5 * 60_000);
        expect(lost).toHaveBeenCalledOnce();
        expect(restored).not.toHaveBeenCalled();
        await vi.advanceTimersByTimeAsync(70_000);

        expect(restored).toHaveBeenCalledOnce();
        expect(fake.mocks.start).toHaveBeenCalledTimes(1 + 41);
    });

    it('tries at once when the page wakes up during a wait', async () => {
        const fake = fakeTransport();
        const wake = fakeWake();
        const connection = createGameConnection(fake.transport, wake.source);
        const restored = vi.fn();
        connection.onReconnected(restored);
        await connection.start();
        // Attempts at 0, 0, 1, 3 and 8 s fail, then one more, after the next wake-up.
        fake.failStarts(6);
        fake.close();
        await vi.advanceTimersByTimeAsync(1_000 + 2_000 + 5_000);
        const attempts = fake.mocks.start.mock.calls.length;

        wake.wake();
        await vi.advanceTimersByTimeAsync(0);
        wake.wake();
        await vi.advanceTimersByTimeAsync(0);

        expect(fake.mocks.start).toHaveBeenCalledTimes(attempts + 2);
        expect(restored).toHaveBeenCalledOnce();
    });

    it('cuts the wait of SignalR short when the page wakes up while it reconnects', async () => {
        const fake = fakeTransport();
        const wake = fakeWake();
        const connection = createGameConnection(fake.transport, wake.source);
        const restored = vi.fn();
        connection.onReconnected(restored);
        await connection.start();
        fake.loseConnection();

        wake.wake();
        await vi.advanceTimersByTimeAsync(0);

        expect(fake.mocks.stop).toHaveBeenCalledOnce();
        expect(fake.mocks.start).toHaveBeenCalledTimes(2);
        expect(restored).toHaveBeenCalledOnce();
    });

    it('ignores a wake-up while connected', async () => {
        const fake = fakeTransport();
        const wake = fakeWake();
        const connection = createGameConnection(fake.transport, wake.source);
        await connection.start();

        wake.wake();
        await vi.advanceTimersByTimeAsync(0);

        expect(fake.mocks.stop).not.toHaveBeenCalled();
        expect(fake.mocks.start).toHaveBeenCalledOnce();
    });

    it('stops trying and listening once stopped', async () => {
        const fake = fakeTransport();
        const wake = fakeWake();
        const connection = createGameConnection(fake.transport, wake.source);
        const lost = vi.fn();
        const restored = vi.fn();
        connection.onReconnecting(lost);
        connection.onReconnected(restored);
        await connection.start();
        fake.failStarts(3);
        fake.close();
        lost.mockClear();

        await connection.stop();
        await vi.advanceTimersByTimeAsync(60_000);

        expect(wake.listening()).toBe(false);
        expect(restored).not.toHaveBeenCalled();
        expect(lost).not.toHaveBeenCalled();
        expect(fake.mocks.start.mock.calls.length).toBeLessThanOrEqual(3);
    });

    it('rejects the start when stopped before the server ever answered', async () => {
        const fake = fakeTransport();
        fake.failStarts(10);
        const connection = createGameConnection(fake.transport, fakeWake().source);

        const started = connection.start();
        await vi.advanceTimersByTimeAsync(1_000);
        await connection.stop();

        await expect(started).rejects.toThrow();
    });
});
