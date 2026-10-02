import { flushSync } from 'svelte';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import type { DisplaySnapshot, GameId } from '../contracts';
import { ConnectionStatus, reconnectingNoticeDelay } from './connectionStatus.svelte';
import { SnapshotStore } from './snapshotStore.svelte';

const gameId = '6f9619ff-8b86-d011-b42d-00cf4fc964ff' as GameId;

function snapshot(version: number): DisplaySnapshot {
    return {
        gameId,
        version,
        phase: 'Lobby',
        joinAddress: '10.0.0.1',
        players: [],
        round: null,
        roundView: null,
    };
}

/** A page synchronized as long as its snapshot is fresh, as the TV screen is. */
function page() {
    const store = new SnapshotStore<DisplaySnapshot>();
    const status = new ConnectionStatus(() => store.fresh);
    const stop = status.start();
    let version = 0;
    return {
        status,
        stop,
        /** A fresh snapshot arrives. */
        synchronize: () => {
            store.accept(snapshot(++version));
            flushSync();
        },
        /** The connection is lost: the snapshot shown may be outdated. */
        lose: () => {
            store.markStale();
            flushSync();
        },
        /** Time passes, with every effect it triggers applied. */
        wait: (milliseconds: number) => {
            vi.advanceTimersByTime(milliseconds);
            flushSync();
        },
    };
}

describe('ConnectionStatus', () => {
    beforeEach(() => {
        vi.useFakeTimers();
    });

    afterEach(() => {
        vi.useRealTimers();
    });

    it('starts interrupted, locked and silent, until the page synchronizes', () => {
        const { status } = page();
        flushSync();

        expect(status.state).toBe('interrupted');
        expect(status.interactive).toBe(false);
    });

    it('shows the notice when the first connection takes longer than the delay', () => {
        const { status, wait } = page();
        flushSync();

        wait(reconnectingNoticeDelay - 1);
        expect(status.state).toBe('interrupted');
        wait(1);
        expect(status.state).toBe('reconnecting');
        expect(status.interactive).toBe(false);
    });

    it('becomes connected and interactive once synchronized', () => {
        const { status, synchronize } = page();

        synchronize();

        expect(status.state).toBe('connected');
        expect(status.interactive).toBe(true);
    });

    it('locks at once when the connection is lost', () => {
        const { status, synchronize, lose } = page();
        synchronize();

        lose();

        expect(status.state).toBe('interrupted');
        expect(status.interactive).toBe(false);
    });

    it('never shows the notice for an outage shorter than the delay', () => {
        const { status, synchronize, lose, wait } = page();
        synchronize();

        lose();
        wait(reconnectingNoticeDelay - 1);
        synchronize();
        wait(reconnectingNoticeDelay);

        expect(status.state).toBe('connected');
    });

    it('shows the notice once the outage lasts longer than the delay', () => {
        const { status, synchronize, lose, wait } = page();
        synchronize();

        lose();
        wait(reconnectingNoticeDelay);

        expect(status.state).toBe('reconnecting');
        expect(status.interactive).toBe(false);
    });

    it('does not restart the delay when the connection is lost again during an outage', () => {
        const { status, synchronize, lose, wait } = page();
        synchronize();

        lose();
        wait(reconnectingNoticeDelay - 1_000);
        lose();
        wait(1_000);

        expect(status.state).toBe('reconnecting');
    });

    it('hides the notice and unlocks once a fresh snapshot arrives', () => {
        const { status, synchronize, lose, wait } = page();
        synchronize();
        lose();
        wait(reconnectingNoticeDelay);

        synchronize();

        expect(status.state).toBe('connected');
        expect(status.interactive).toBe(true);
    });

    it('measures each outage from its own start', () => {
        const { status, synchronize, lose, wait } = page();
        synchronize();
        lose();
        wait(reconnectingNoticeDelay);
        synchronize();

        lose();

        expect(status.state).toBe('interrupted');
        wait(reconnectingNoticeDelay - 1);
        expect(status.state).toBe('interrupted');
        wait(1);
        expect(status.state).toBe('reconnecting');
    });

    it('no longer shows the notice once stopped', () => {
        const { status, synchronize, lose, wait, stop } = page();
        synchronize();
        lose();

        stop();
        wait(reconnectingNoticeDelay);

        expect(status.state).toBe('interrupted');
    });
});
