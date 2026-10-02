/**
 * One round trip to the server, as NTP measures it. Every time is in milliseconds since the Unix
 * epoch: the local ones are taken with `performance.now()` and brought back to the epoch with
 * `performance.timeOrigin`, so that a change of the system clock does not affect them.
 */
export interface ClockSample {
    /** Local time when the request left. */
    readonly sentAt: number;
    /** Time of the server when it answered. */
    readonly serverTime: number;
    /** Local time when the answer arrived. */
    readonly receivedAt: number;
}

/** How the clock of the server relates to the local one. */
export interface ClockEstimate {
    /** What to add to a local time to get the time of the server, in milliseconds. */
    readonly offset: number;
    /** The shortest round trip of the burst, in milliseconds: the offset is within half of it. */
    readonly roundTrip: number;
}

/** The round trip of a sample: the time its request and its answer spent on the network. */
export function roundTripOf(sample: ClockSample): number {
    return sample.receivedAt - sample.sentAt;
}

/**
 * The offset a sample gives, assuming its request and its answer took as long: the server
 * answered halfway through the round trip.
 */
export function offsetOf(sample: ClockSample): number {
    return sample.serverTime - (sample.sentAt + sample.receivedAt) / 2;
}

/**
 * Estimates the clock of the server from a burst of samples, or returns null without any. Only
 * the quarter of the samples with the shortest round trips counts (more on a tie): a slow round
 * trip is one the Wi-Fi delayed, most likely in one direction only, and its offset may be wrong by
 * up to half of it. The median of their offsets then sets aside what remains of the noise.
 */
export function estimateClock(samples: readonly ClockSample[]): ClockEstimate | null {
    const roundTrips = samples.map(roundTripOf).sort((a, b) => a - b);
    const quickest = roundTrips[0];
    // Samples as fast as the slowest one kept count as well, whatever their order.
    const cutoff = roundTrips[Math.ceil(roundTrips.length / 4) - 1];
    if (quickest === undefined || cutoff === undefined) {
        return null;
    }
    const fastest = samples.filter((sample) => roundTripOf(sample) <= cutoff);
    return { offset: median(fastest.map(offsetOf)), roundTrip: quickest };
}

function median(values: readonly number[]): number {
    const sorted = [...values].sort((a, b) => a - b);
    const middle = Math.floor(sorted.length / 2);
    const upper = sorted[middle] ?? 0;
    return sorted.length % 2 === 1 ? upper : ((sorted[middle - 1] ?? upper) + upper) / 2;
}
