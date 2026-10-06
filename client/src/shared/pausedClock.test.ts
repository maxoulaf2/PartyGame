import { describe, expect, it } from 'vitest';
import type { ServerClock } from './connection/clockSync.svelte';
import { pausedClock } from './pausedClock';

const clock: ServerClock = {
    synchronized: true,
    serverNow: () => 1_790_000_020_000,
    toServerTime: (timestamp) => 1_790_000_000_000 + timestamp,
};

describe('pausedClock', () => {
    it('is the server clock while the game is not paused', () => {
        expect(pausedClock(clock, null)).toBe(clock);
    });

    it('stands still at the start of the pause', () => {
        const paused = pausedClock(clock, 1_790_000_008_000);

        expect([paused.serverNow(), paused.toServerTime(123), paused.synchronized]).toEqual([
            1_790_000_008_000,
            1_790_000_008_000,
            true,
        ]);
    });
});
