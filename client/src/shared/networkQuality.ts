import type { ConnectionQuality, ConnectionTransport, DiagnosticVerdict } from './contracts';

// Thresholds of the network diagnostic (US-E12-04), adjustable without a new decision.

/** Median round trip below which the network is good, in milliseconds. */
export const roundTripGood = 50;
/** Median round trip up to which the network is usable, in milliseconds: a problem beyond. */
export const roundTripUsable = 150;
/** Share of lost round trips beyond which the network is unstable. */
export const lossesUsable = 0.02;
/** Throughput below which the images of the pack load slowly, in Mbit/s. */
export const throughputGood = 2;
/**
 * Reconnections of a player beyond which the console marks them: a phone reconnects each time it
 * wakes up, so a few are expected in an evening.
 */
export const reconnectionsNotable = 5;
/**
 * Uncertainty of the clock of a phone beyond which the console marks it, in milliseconds
 * (US-E13-06): beyond, two buzzes this close may be told apart wrongly.
 */
export const clockUncertaintyNotable = 50;

/**
 * How far the estimated time of the server may be from the true one, in milliseconds: half the
 * shortest round trip of the clock burst, since the server answered somewhere within it.
 */
export function clockUncertainty(roundTrip: number): number {
    return roundTrip / 2;
}

/** The round trips of a burst, in milliseconds. */
export interface RoundTripSummary {
    readonly median: number;
    readonly max: number;
    /** The mean difference between two consecutive round trips, as RTP measures it. */
    readonly jitter: number;
}

/** Summarizes the round trips of a burst, or returns null without any. */
export function summarizeRoundTrips(roundTrips: readonly number[]): RoundTripSummary | null {
    if (roundTrips.length === 0) {
        return null;
    }
    const sorted = [...roundTrips].sort((a, b) => a - b);
    const middle = Math.floor(sorted.length / 2);
    const median =
        sorted.length % 2 === 1
            ? (sorted[middle] ?? 0)
            : ((sorted[middle - 1] ?? 0) + (sorted[middle] ?? 0)) / 2;
    const differences = roundTrips
        .slice(1)
        .map((roundTrip, i) => Math.abs(roundTrip - (roundTrips[i] ?? roundTrip)));
    const jitter =
        differences.length === 0 ? 0 : differences.reduce((a, b) => a + b, 0) / differences.length;
    return { median, max: sorted[sorted.length - 1] ?? 0, jitter };
}

/** What the diagnostic page measured. */
export interface DiagnosticMeasures {
    /** How the page reaches the hub, or null when it could not connect. */
    readonly transport: ConnectionTransport | null;
    /** Whether the phone is on the network of the advertised address, null when unknown. */
    readonly sameSubnet: boolean | null;
    /** The round trips of the burst, or null when none came back. */
    readonly roundTrips: RoundTripSummary | null;
    /** Round trips sent during the stability test, and how many never came back in time. */
    readonly pings: number;
    readonly lost: number;
    /** Connections lost during the test. */
    readonly reconnections: number;
    /** Throughput of the download, in Mbit/s, or null when it failed. */
    readonly throughput: number | null;
}

/** A measure in default, each with its advice on the page. */
export type DiagnosticFinding =
    | 'unreachable'
    | 'roundTripHigh'
    | 'roundTripTooHigh'
    | 'losses'
    | 'reconnections'
    | 'fallbackTransport'
    | 'otherNetwork'
    | 'slowDownload'
    | 'downloadFailed';

export interface DiagnosticOutcome {
    readonly verdict: DiagnosticVerdict;
    readonly findings: readonly DiagnosticFinding[];
}

/** The findings that keep the phone from playing; any other one is a reservation. */
const problems: readonly DiagnosticFinding[] = ['unreachable', 'roundTripTooHigh'];

/** Decides the verdict of a diagnostic from its measures, with the measures in default. */
export function assessDiagnostic(measures: DiagnosticMeasures): DiagnosticOutcome {
    const { transport, roundTrips } = measures;
    if (transport === null || roundTrips === null) {
        // Nothing else means anything without the hub.
        return { verdict: 'Problem', findings: ['unreachable'] };
    }
    const findings: DiagnosticFinding[] = [];
    if (roundTrips.median > roundTripUsable) {
        findings.push('roundTripTooHigh');
    } else if (roundTrips.median >= roundTripGood) {
        findings.push('roundTripHigh');
    }
    if (measures.pings > 0 && measures.lost / measures.pings > lossesUsable) {
        findings.push('losses');
    }
    if (measures.reconnections > 0) {
        findings.push('reconnections');
    }
    if (transport !== 'WebSockets') {
        findings.push('fallbackTransport');
    }
    if (measures.sameSubnet === false) {
        findings.push('otherNetwork');
    }
    if (measures.throughput === null) {
        findings.push('downloadFailed');
    } else if (measures.throughput < throughputGood) {
        findings.push('slowDownload');
    }
    const verdict = findings.some((finding) => problems.includes(finding))
        ? 'Problem'
        : findings.length > 0
          ? 'Reserved'
          : 'Good';
    return { verdict, findings };
}

/** Which measures of a connection the game master console marks as poor. */
export interface ConnectionWarnings {
    readonly roundTrip: boolean;
    readonly clockUncertainty: boolean;
    readonly transport: boolean;
    readonly reconnections: boolean;
}

export function connectionWarnings(quality: ConnectionQuality): ConnectionWarnings {
    return {
        roundTrip: quality.roundTrip !== null && quality.roundTrip > roundTripUsable,
        clockUncertainty:
            quality.roundTrip !== null &&
            clockUncertainty(quality.roundTrip) > clockUncertaintyNotable,
        transport: quality.transport !== 'WebSockets',
        reconnections: quality.reconnections > reconnectionsNotable,
    };
}
