import type { ClockSync } from './clockSync.svelte';
import type { GameConnection } from './gameHub';

/**
 * Tells the server the round trip of every clock burst, for the game master console to show how
 * well the page reaches the server. A report that cannot be sent is dropped: the next burst, a
 * minute later or at the next connection, sends another. Returns a function that stops.
 */
export function reportRoundTrips(
    connection: GameConnection,
    clock: Pick<ClockSync, 'roundTrip'>,
): () => void {
    return $effect.root(() => {
        $effect(() => {
            // Read on every burst: each one replaces the estimate, even with the same round trip.
            const roundTrip = clock.roundTrip;
            if (roundTrip !== null) {
                connection
                    .invoke('ReportConnectionQuality', { roundTrip: Math.round(roundTrip) })
                    .catch(() => {});
            }
        });
    });
}
