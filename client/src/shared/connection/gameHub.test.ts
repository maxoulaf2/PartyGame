import { describe, expect, it, vi } from 'vitest';
import type { AnnouncementResult } from '../contracts';
import { createGameConnection, type HubTransport } from './gameHub';

function fakeTransport(answer: unknown = null) {
    const mocks = {
        start: vi.fn(async () => {}),
        stop: vi.fn(async () => {}),
        invoke: vi.fn<(method: string, ...args: unknown[]) => Promise<unknown>>(async () => answer),
        on: vi.fn(),
        off: vi.fn(),
        onreconnecting: vi.fn<(callback: (error?: Error) => void) => void>(),
        onreconnected: vi.fn<(callback: (connectionId?: string) => void) => void>(),
        onclose: vi.fn<(callback: (error?: Error) => void) => void>(),
    };
    // SignalR types invoke as generic in its answer, which a mock cannot express.
    const transport: HubTransport = {
        ...mocks,
        invoke: mocks.invoke as unknown as HubTransport['invoke'],
    };
    return { transport, mocks };
}

interface TestMessages {
    Moved(position: number, label: string): void;
}

describe('createGameConnection', () => {
    it('starts and stops the underlying connection', async () => {
        const { transport, mocks } = fakeTransport();
        const connection = createGameConnection(transport);

        await connection.start();
        await connection.stop();

        expect(mocks.start).toHaveBeenCalledOnce();
        expect(mocks.stop).toHaveBeenCalledOnce();
    });

    it('invokes a hub method by its name and resolves to its answer', async () => {
        const answer: AnnouncementResult = { refusal: 'GameMasterCodeInvalid' };
        const { transport, mocks } = fakeTransport(answer);
        const connection = createGameConnection(transport);

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
        const connection = createGameConnection<TestMessages>(transport);
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

    it('reports when the connection is lost, restored and closed', () => {
        const { transport, mocks } = fakeTransport();
        const connection = createGameConnection(transport);
        const reconnecting = vi.fn();
        const reconnected = vi.fn();
        const closed = vi.fn();

        connection.onReconnecting(reconnecting);
        connection.onReconnected(reconnected);
        connection.onClose(closed);
        mocks.onreconnecting.mock.calls[0]?.[0](new Error('lost'));
        mocks.onreconnected.mock.calls[0]?.[0]('new-id');
        mocks.onclose.mock.calls[0]?.[0]();

        expect(reconnecting).toHaveBeenCalledOnce();
        expect(reconnected).toHaveBeenCalledOnce();
        expect(closed).toHaveBeenCalledOnce();
    });
});
