import { describe, expect, it, vi } from 'vitest';
import type { IGameClient, Welcome } from '../contracts';
import { decideOnBuild, watchBuild, type BuildWatchOptions } from './buildCheck';
import type { CodeStorage } from './codeStorage';
import type { GameConnection } from './gameHub';

describe('decideOnBuild', () => {
    it('keeps the page when both builds are the same', () => {
        expect(decideOnBuild('b1', 'b1', null)).toBe('keep');
    });

    it('reloads the page when the server serves another build', () => {
        expect(decideOnBuild('b1', 'b2', null)).toBe('reload');
    });

    it('reports instead of reloading again when the page already reloaded for that build', () => {
        expect(decideOnBuild('b1', 'b2', 'b2')).toBe('report');
    });

    it('reloads again when the page reloaded for an older build than the one served now', () => {
        expect(decideOnBuild('b1', 'b3', 'b2')).toBe('reload');
    });

    it('keeps the page when the server has no build identifier', () => {
        expect(decideOnBuild('b1', null, null)).toBe('keep');
    });

    it('keeps the page when the page has no build identifier, as with npm run dev', () => {
        expect(decideOnBuild(null, 'b2', null)).toBe('keep');
    });
});

function fakeConnection(invokeFails = false) {
    let welcome: ((welcome: Welcome) => void) | null = null;
    const unsubscribe = vi.fn();
    const connection = {
        on: vi.fn((message: string, handler: (welcome: Welcome) => void) => {
            if (message === 'ReceiveWelcome') {
                welcome = handler;
            }
            return unsubscribe;
        }),
        invoke: vi.fn(() =>
            invokeFails ? Promise.reject(new Error('disconnected')) : Promise.resolve(null),
        ),
    };
    // The fake only implements what watchBuild uses, with loose signatures.
    const typed = connection as unknown as GameConnection<IGameClient>;
    return {
        connection,
        typed,
        unsubscribe,
        welcome: (buildId: string | null) => welcome?.({ buildId, gamePending: false }),
        subscribed: () => welcome !== null,
    };
}

/** The `sessionStorage` of a tab, which outlives the reloads of the page. */
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

/** A browser that refuses storage: nothing is ever kept. */
const blockedStorage: CodeStorage = { load: () => null, save: () => {}, clear: () => {} };

function options(overrides: Partial<BuildWatchOptions> = {}) {
    return { reload: vi.fn(), reloadTarget: memoryStorage(), pageBuild: 'b1', ...overrides };
}

describe('watchBuild', () => {
    it('does nothing when the server serves the build of the page', () => {
        const { connection, typed, welcome } = fakeConnection();
        const page = options();

        watchBuild(typed, page);
        welcome('b1');

        expect(page.reload).not.toHaveBeenCalled();
        expect(connection.invoke).not.toHaveBeenCalled();
    });

    it('reloads once, remembering the build it reloads for, when the server serves another build', () => {
        const { typed, welcome } = fakeConnection();
        const reloadTarget = memoryStorage();
        const page = options({ reloadTarget });

        watchBuild(typed, page);
        welcome('b2');

        expect(page.reload).toHaveBeenCalledOnce();
        expect(reloadTarget.value).toBe('b2');
    });

    it('does not reload again after a reload that brought back the same outdated page', () => {
        const { connection, typed, welcome } = fakeConnection();
        const page = options({ reloadTarget: memoryStorage('b2') });

        watchBuild(typed, page);
        welcome('b2');

        expect(page.reload).not.toHaveBeenCalled();
        expect(connection.invoke).toHaveBeenCalledExactlyOnceWith('ReportStaleBuild', {
            clientBuildId: 'b1',
        });
    });

    it('reports a persisting gap once per page, however many reconnections', () => {
        const { connection, typed, welcome } = fakeConnection();
        const page = options({ reloadTarget: memoryStorage('b2') });

        watchBuild(typed, page);
        welcome('b2');
        welcome('b2');
        welcome('b2');

        expect(connection.invoke).toHaveBeenCalledOnce();
    });

    it('reports again at the next welcome when the report could not be sent', async () => {
        const { connection, typed, welcome } = fakeConnection(true);
        const page = options({ reloadTarget: memoryStorage('b2') });

        watchBuild(typed, page);
        welcome('b2');
        await vi.waitFor(() => expect(connection.invoke).toHaveBeenCalledOnce());
        await Promise.resolve();
        welcome('b2');

        expect(connection.invoke).toHaveBeenCalledTimes(2);
    });

    it('reloads again when the server is updated once more after a failed reload', () => {
        const { typed, welcome } = fakeConnection();
        const reloadTarget = memoryStorage('b2');
        const page = options({ reloadTarget });

        watchBuild(typed, page);
        welcome('b3');

        expect(page.reload).toHaveBeenCalledOnce();
        expect(reloadTarget.value).toBe('b3');
    });

    it('forgets the reload once the page runs the build served', () => {
        const { typed, welcome } = fakeConnection();
        const reloadTarget = memoryStorage('b2');

        watchBuild(typed, options({ pageBuild: 'b2', reloadTarget }));
        welcome('b2');

        expect(reloadTarget.value).toBeNull();
    });

    it('never reloads when the server has no build identifier', () => {
        const { connection, typed, welcome } = fakeConnection();
        const page = options();

        watchBuild(typed, page);
        welcome(null);

        expect(page.reload).not.toHaveBeenCalled();
        expect(connection.invoke).not.toHaveBeenCalled();
    });

    it('is inactive when the page has no build identifier, as with npm run dev', () => {
        const { typed, welcome, subscribed } = fakeConnection();
        const page = options({ pageBuild: null });

        watchBuild(typed, page);
        welcome('b2');

        expect(subscribed()).toBe(false);
        expect(page.reload).not.toHaveBeenCalled();
    });

    it('reports instead of reloading when the browser refuses storage, which could not stop a loop', () => {
        const { connection, typed, welcome } = fakeConnection();
        const page = options({ reloadTarget: blockedStorage });

        watchBuild(typed, page);
        welcome('b2');

        expect(page.reload).not.toHaveBeenCalled();
        expect(connection.invoke).toHaveBeenCalledOnce();
    });

    it('stops watching when asked', () => {
        const { typed, unsubscribe } = fakeConnection();

        const stop = watchBuild(typed, options());
        stop();

        expect(unsubscribe).toHaveBeenCalledOnce();
    });
});
