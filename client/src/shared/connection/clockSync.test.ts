import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import type { IGameClient } from '../contracts';
import {
    ClockSync,
    clockSyncBurstSize,
    clockSyncInterval,
    clockSyncSpacing,
    type LocalClock,
} from './clockSync.svelte';
import type { GameConnection } from './gameHub';

/** The page loaded at 20:00:00 UTC, by its own clock. */
const origin = Date.UTC(2026, 9, 2, 20, 0, 0);
/** The server clock runs 2.5 s ahead of the phone. */
const serverAhead = 2_500;
/** Each way of a round trip takes 10 ms. */
const latency = 10;

/** A local clock driven by the fake timers, as `performance.now()` is by real time. */
const localClock: LocalClock = { origin, now: () => Date.now() - origin };

/** A server reached through a connection whose round trips take 2 × `latency` ms. */
function fakeConnection() {
    const callbacks = { connected: [] as (() => void)[], lost: [] as (() => void)[] };
    let reachable = true;
    const invoke = vi.fn((method: string) => {
        if (method !== 'SyncClock') {
            return Promise.reject(new Error(`unexpected ${method}`));
        }
        if (!reachable) {
            return Promise.reject(new Error('disconnected'));
        }
        const serverTime = Date.now() + latency + serverAhead;
        return new Promise((resolve) => setTimeout(() => resolve({ serverTime }), 2 * latency));
    });
    const connection = {
        invoke,
        onConnected: vi.fn((callback: () => void) => callbacks.connected.push(callback)),
        onReconnecting: vi.fn((callback: () => void) => callbacks.lost.push(callback)),
    };
    // The fake only implements what ClockSync uses, with loose signatures.
    const typed = connection as unknown as GameConnection<IGameClient>;
    return {
        typed,
        invoke,
        connect: () => {
            reachable = true;
            callbacks.connected.forEach((callback) => callback());
        },
        lose: () => {
            reachable = false;
            callbacks.lost.forEach((callback) => callback());
        },
    };
}

/** Time a whole burst takes with the fake connection. */
const burstDuration =
    clockSyncBurstSize * 2 * latency + (clockSyncBurstSize - 1) * clockSyncSpacing;

describe('ClockSync', () => {
    beforeEach(() => {
        vi.useFakeTimers({ now: origin + 5_000 });
    });

    afterEach(() => {
        vi.useRealTimers();
    });

    it('assumes both clocks agree until the first burst completes', () => {
        const sync = new ClockSync(fakeConnection().typed, localClock);

        expect(sync.synchronized).toBe(false);
        expect(sync.offset).toBe(0);
        expect(sync.roundTrip).toBeNull();
        expect(sync.serverNow()).toBe(Date.now());
    });

    it('makes a burst of 8 spaced round trips once connected, then knows the server time', async () => {
        const fake = fakeConnection();
        const sync = new ClockSync(fake.typed, localClock);
        sync.start();

        fake.connect();
        await vi.advanceTimersByTimeAsync(2 * latency);
        expect(fake.invoke).toHaveBeenCalledOnce();
        await vi.advanceTimersByTimeAsync(clockSyncSpacing - 1);
        expect(fake.invoke).toHaveBeenCalledOnce();
        await vi.advanceTimersByTimeAsync(burstDuration);

        expect(fake.invoke).toHaveBeenCalledTimes(clockSyncBurstSize);
        expect(sync.synchronized).toBe(true);
        expect(sync.offset).toBe(serverAhead);
        expect(sync.roundTrip).toBe(2 * latency);
        expect(sync.serverNow()).toBe(Date.now() + serverAhead);
    });

    it('converts performance timestamps to server times and back', async () => {
        const fake = fakeConnection();
        const sync = new ClockSync(fake.typed, localClock);
        sync.start();
        fake.connect();
        await vi.advanceTimersByTimeAsync(burstDuration);

        expect(sync.toServerTime(1_234.5)).toBe(origin + 1_234.5 + serverAhead);
        expect(sync.toLocalTime(origin + 1_234.5 + serverAhead)).toBe(1_234.5);
    });

    it('is not affected by a change of the wall clock', async () => {
        const fake = fakeConnection();
        const frozen = localClock.now();
        const sync = new ClockSync(fake.typed, { origin, now: () => frozen });
        sync.start();
        fake.connect();
        await vi.advanceTimersByTimeAsync(burstDuration);
        const before = sync.serverNow();

        // The user sets the phone an hour back: only Date.now() moves, not performance.now().
        vi.setSystemTime(Date.now() - 3_600_000);

        expect(sync.serverNow()).toBe(before);
    });

    it('synchronizes again every minute', async () => {
        const fake = fakeConnection();
        const sync = new ClockSync(fake.typed, localClock);
        sync.start();
        fake.connect();
        await vi.advanceTimersByTimeAsync(burstDuration);

        await vi.advanceTimersByTimeAsync(clockSyncInterval - 1);
        expect(fake.invoke).toHaveBeenCalledTimes(clockSyncBurstSize);
        await vi.advanceTimersByTimeAsync(1 + burstDuration);

        expect(fake.invoke).toHaveBeenCalledTimes(2 * clockSyncBurstSize);
    });

    it('is no longer synchronized once the connection is lost, and synchronizes again when it is back', async () => {
        const fake = fakeConnection();
        const sync = new ClockSync(fake.typed, localClock);
        sync.start();
        fake.connect();
        await vi.advanceTimersByTimeAsync(burstDuration);

        fake.lose();
        expect(sync.synchronized).toBe(false);
        // The previous estimate remains the best guess meanwhile.
        expect(sync.offset).toBe(serverAhead);
        await vi.advanceTimersByTimeAsync(5_000);
        fake.connect();
        await vi.advanceTimersByTimeAsync(burstDuration);

        expect(fake.invoke).toHaveBeenCalledTimes(2 * clockSyncBurstSize);
        expect(sync.synchronized).toBe(true);
    });

    it('keeps the previous estimate when a burst fails', async () => {
        const fake = fakeConnection();
        const sync = new ClockSync(fake.typed, localClock);
        sync.start();
        fake.connect();
        await vi.advanceTimersByTimeAsync(burstDuration);

        // The minute elapses while the server cannot be reached.
        fake.lose();
        await vi.advanceTimersByTimeAsync(clockSyncInterval + burstDuration);

        expect(fake.invoke).toHaveBeenCalledTimes(2 * clockSyncBurstSize);
        expect(sync.offset).toBe(serverAhead);
        expect(sync.roundTrip).toBe(2 * latency);
    });

    it('ignores a lost round trip', async () => {
        const fake = fakeConnection();
        fake.invoke.mockRejectedValueOnce(new Error('lost'));
        const sync = new ClockSync(fake.typed, localClock);
        sync.start();

        fake.connect();
        await vi.advanceTimersByTimeAsync(burstDuration);

        expect(fake.invoke).toHaveBeenCalledTimes(clockSyncBurstSize);
        expect(sync.synchronized).toBe(true);
        expect(sync.offset).toBe(serverAhead);
    });

    it('keeps the previous estimate when the connection drops during a burst', async () => {
        const fake = fakeConnection();
        const sync = new ClockSync(fake.typed, localClock);
        sync.start();
        fake.connect();
        await vi.advanceTimersByTimeAsync(burstDuration);
        await vi.advanceTimersByTimeAsync(clockSyncInterval);

        // Half the next burst done, the connection drops, and the other half fails.
        await vi.advanceTimersByTimeAsync(burstDuration / 2);
        fake.lose();
        await vi.advanceTimersByTimeAsync(burstDuration);

        expect(sync.synchronized).toBe(false);
        expect(sync.offset).toBe(serverAhead);
    });

    it('makes another burst after the current one when the connection comes back meanwhile', async () => {
        const fake = fakeConnection();
        const sync = new ClockSync(fake.typed, localClock);
        sync.start();
        fake.connect();
        await vi.advanceTimersByTimeAsync(burstDuration / 2);

        fake.lose();
        fake.connect();
        await vi.advanceTimersByTimeAsync(burstDuration);
        expect(sync.synchronized).toBe(false);
        await vi.advanceTimersByTimeAsync(burstDuration);

        expect(sync.synchronized).toBe(true);
        expect(fake.invoke).toHaveBeenCalledTimes(2 * clockSyncBurstSize);
    });

    it('stops synchronizing once stopped', async () => {
        const fake = fakeConnection();
        const sync = new ClockSync(fake.typed, localClock);
        const stop = sync.start();
        fake.connect();
        await vi.advanceTimersByTimeAsync(burstDuration);

        stop();
        fake.connect();
        await vi.advanceTimersByTimeAsync(10 * clockSyncInterval);

        expect(fake.invoke).toHaveBeenCalledTimes(clockSyncBurstSize);
    });
});
