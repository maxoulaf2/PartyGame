import type { ServerClock } from './connection/clockSync.svelte';

/**
 * The clock the views of a game see: the server clock, or, while the game master pauses the game,
 * a clock stopped at `pausedAt`, so that every countdown stands still with the time it had left.
 */
export function pausedClock(clock: ServerClock, pausedAt: number | null): ServerClock {
    if (pausedAt === null) {
        return clock;
    }
    return {
        get synchronized() {
            return clock.synchronized;
        },
        serverNow: () => pausedAt,
        toServerTime: () => pausedAt,
    };
}
