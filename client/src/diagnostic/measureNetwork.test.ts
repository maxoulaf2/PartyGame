import { describe, expect, it } from 'vitest';
import {
    burstSize,
    measureNetwork,
    stabilitySeconds,
    type DiagnosticStep,
    type NetworkProbe,
} from './measureNetwork';

/**
 * A network whose round trips come from `roundTrips` in turn, null being lost, on a fake clock,
 * with the connections lost so far given by `reconnections`.
 */
function fakeProbe(
    roundTrips: (number | null)[],
    options: Partial<Omit<NetworkProbe, 'reconnections'>> = {},
    reconnections: () => number = () => 0,
) {
    let time = 0;
    let call = 0;
    const probe: NetworkProbe = {
        get reconnections() {
            return reconnections();
        },
        waitConnected: () => Promise.resolve(true),
        checkNetwork: () => Promise.resolve({ transport: 'WebSockets', sameSubnet: true }),
        roundTrip: (timeout) => {
            const roundTrip = roundTrips[call++ % roundTrips.length] ?? null;
            time += roundTrip ?? timeout;
            return Promise.resolve(roundTrip);
        },
        download: () => Promise.resolve(42),
        sleep: (ms) => {
            time += ms;
            return Promise.resolve();
        },
        now: () => time,
        ...options,
    };
    return { probe, elapsed: () => time };
}

describe('measureNetwork', () => {
    it('measures a burst, then one round trip a second, then the download', async () => {
        const { probe, elapsed } = fakeProbe([10, 30, 20]);
        const steps: DiagnosticStep[] = [];

        const measures = await measureNetwork(probe, (step) => steps.push(step));

        expect(steps).toEqual(['connecting', 'roundTrips', 'stability', 'download']);
        expect(measures).toMatchObject({
            transport: 'WebSockets',
            sameSubnet: true,
            pings: stabilitySeconds,
            lost: 0,
            reconnections: 0,
            throughput: 42,
        });
        expect(measures.roundTrips?.median).toBe(20);
        // About 20 s in all, whatever the round trips.
        expect(elapsed()).toBeGreaterThanOrEqual(stabilitySeconds * 1_000);
        expect(elapsed()).toBeLessThan(stabilitySeconds * 1_000 + 2_000);
    });

    it('counts the round trips lost and the connections lost during the test', async () => {
        let reconnections = 0;
        // Every other round trip of the stability test is lost.
        const pattern = [
            ...Array<number>(burstSize).fill(10),
            ...Array.from({ length: stabilitySeconds }, (_, i) => (i % 2 === 0 ? null : 10)),
        ];
        const { probe } = fakeProbe(
            pattern,
            {
                waitConnected: () => {
                    reconnections = 3; // before the test: not counted
                    return Promise.resolve(true);
                },
                checkNetwork: () => Promise.resolve({ transport: 'LongPolling', sameSubnet: null }),
                download: () => {
                    reconnections++;
                    return Promise.resolve(null);
                },
            },
            () => reconnections,
        );

        const measures = await measureNetwork(probe, () => {});

        expect(measures).toMatchObject({ transport: 'LongPolling', lost: 8, reconnections: 1 });
        expect(measures.throughput).toBeNull();
    });

    it('stops at once when the hub stays out of reach', async () => {
        const { probe } = fakeProbe([10], { waitConnected: () => Promise.resolve(false) });
        const steps: DiagnosticStep[] = [];

        const measures = await measureNetwork(probe, (step) => steps.push(step));

        expect(steps).toEqual(['connecting']);
        expect(measures.transport).toBeNull();
    });
});
