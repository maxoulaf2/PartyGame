import { describe, expect, it } from 'vitest';
import {
    assessDiagnostic,
    connectionWarnings,
    reconnectionsNotable,
    summarizeRoundTrips,
    type DiagnosticMeasures,
} from './networkQuality';

/** A phone on the Wi-Fi of a quiet home. */
const good: DiagnosticMeasures = {
    transport: 'WebSockets',
    sameSubnet: true,
    roundTrips: { median: 12, max: 30, jitter: 3 },
    pings: 15,
    lost: 0,
    reconnections: 0,
    throughput: 40,
};

describe('summarizeRoundTrips', () => {
    it('gives the median, the maximum and the mean difference between consecutive round trips', () => {
        expect(summarizeRoundTrips([10, 30, 20])).toEqual({ median: 20, max: 30, jitter: 15 });
    });

    it('takes the middle of the two central round trips of an even burst', () => {
        expect(summarizeRoundTrips([40, 10, 20, 30])?.median).toBe(25);
    });

    it('has no jitter with a single round trip, and nothing without any', () => {
        expect(summarizeRoundTrips([8])).toEqual({ median: 8, max: 8, jitter: 0 });
        expect(summarizeRoundTrips([])).toBeNull();
    });
});

describe('assessDiagnostic', () => {
    it('finds everything good on a quiet home network', () => {
        expect(assessDiagnostic(good)).toEqual({ verdict: 'Good', findings: [] });
    });

    it('is a problem when the hub is out of reach, whatever else', () => {
        expect(assessDiagnostic({ ...good, transport: null })).toEqual({
            verdict: 'Problem',
            findings: ['unreachable'],
        });
        expect(assessDiagnostic({ ...good, roundTrips: null }).verdict).toBe('Problem');
    });

    it.each([
        [49, 'Good', []],
        [50, 'Reserved', ['roundTripHigh']],
        [150, 'Reserved', ['roundTripHigh']],
        [151, 'Problem', ['roundTripTooHigh']],
    ])('rates a median round trip of %i ms as %s', (median, verdict, findings) => {
        expect(
            assessDiagnostic({ ...good, roundTrips: { median, max: median, jitter: 0 } }),
        ).toEqual({ verdict, findings });
    });

    it('reserves a network that loses more than 2 % of the round trips', () => {
        expect(assessDiagnostic({ ...good, pings: 50, lost: 1 }).verdict).toBe('Good');
        expect(assessDiagnostic({ ...good, pings: 15, lost: 1 })).toEqual({
            verdict: 'Reserved',
            findings: ['losses'],
        });
    });

    it.each([
        [{ reconnections: 1 }, 'reconnections'],
        [{ transport: 'LongPolling' as const }, 'fallbackTransport'],
        [{ sameSubnet: false }, 'otherNetwork'],
        [{ throughput: 1.5 }, 'slowDownload'],
        [{ throughput: null }, 'downloadFailed'],
    ])('reserves a network with %o', (change, finding) => {
        expect(assessDiagnostic({ ...good, ...change })).toEqual({
            verdict: 'Reserved',
            findings: [finding],
        });
    });

    it('trusts the network when the server cannot tell which one the phone is on', () => {
        expect(assessDiagnostic({ ...good, sameSubnet: null }).verdict).toBe('Good');
    });

    it('lists every measure in default, a problem winning over the reservations', () => {
        expect(
            assessDiagnostic({
                ...good,
                roundTrips: { median: 400, max: 900, jitter: 120 },
                reconnections: 2,
                sameSubnet: false,
            }),
        ).toEqual({
            verdict: 'Problem',
            findings: ['roundTripTooHigh', 'reconnections', 'otherNetwork'],
        });
    });
});

describe('connectionWarnings', () => {
    it('marks a slow round trip, a fallback transport and frequent reconnections', () => {
        expect(
            connectionWarnings({
                playerId: null,
                transport: 'ServerSentEvents',
                roundTrip: 151,
                reconnections: reconnectionsNotable + 1,
            }),
        ).toEqual({ roundTrip: true, transport: true, reconnections: true });
    });

    it('marks nothing on a good connection, nor a round trip not measured yet', () => {
        expect(
            connectionWarnings({
                playerId: null,
                transport: 'WebSockets',
                roundTrip: null,
                reconnections: reconnectionsNotable,
            }),
        ).toEqual({ roundTrip: false, transport: false, reconnections: false });
    });
});
