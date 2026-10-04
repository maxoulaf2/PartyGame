import type { NetworkCheckResult } from '../shared/contracts';
import type { GameConnection } from '../shared/connection/gameHub';
import { summarizeRoundTrips, type DiagnosticMeasures } from '../shared/networkQuality';

/** How long the page waits for the hub before it calls it out of reach. */
export const connectTimeout = 10_000;
/** Round trips of the burst that measures the response time, and the pause between two. */
export const burstSize = 10;
export const burstSpacing = 50;
/** Length of the stability test: one round trip a second, lost if it takes longer. */
export const stabilitySeconds = 15;
const pingInterval = 1_000;
/** Served by the server, about 1 MB, never from a cache (`NetworkDiagnosticExtensions`). */
export const downloadUrl = '/api/diagnostic/download';
const downloadTimeout = 10_000;

/** What the page is measuring, to tell the user. */
export type DiagnosticStep = 'connecting' | 'roundTrips' | 'stability' | 'download';

/** What the measures rely on, so that tests can stand in for the network and the clock. */
export interface NetworkProbe {
    /** Resolves to true once connected, or to false after `timeout` ms. */
    waitConnected(timeout: number): Promise<boolean>;
    /** Connections lost since the page opened. */
    readonly reconnections: number;
    /** What the server sees of the connection. */
    checkNetwork(): Promise<NetworkCheckResult>;
    /** One round trip to the hub, in ms, or null when lost or longer than `timeout` ms. */
    roundTrip(timeout: number): Promise<number | null>;
    /** Downloads the test file, and resolves to its throughput in Mbit/s, or null on failure. */
    download(timeout: number): Promise<number | null>;
    sleep(ms: number): Promise<void>;
    /** Monotonic milliseconds. */
    now(): number;
}

const unreachable: DiagnosticMeasures = {
    transport: null,
    sameSubnet: null,
    roundTrips: null,
    pings: 0,
    lost: 0,
    reconnections: 0,
    throughput: null,
};

/** Runs the whole test, in about 20 s, telling each step as it starts. */
export async function measureNetwork(
    probe: NetworkProbe,
    onStep: (step: DiagnosticStep) => void,
): Promise<DiagnosticMeasures> {
    onStep('connecting');
    if (!(await probe.waitConnected(connectTimeout))) {
        return unreachable;
    }
    const reconnectionsBefore = probe.reconnections;
    let check: NetworkCheckResult;
    try {
        check = await probe.checkNetwork();
    } catch {
        return unreachable;
    }

    onStep('roundTrips');
    const roundTrips: number[] = [];
    for (let i = 0; i < burstSize; i++) {
        if (i > 0) {
            await probe.sleep(burstSpacing);
        }
        const roundTrip = await probe.roundTrip(pingInterval);
        if (roundTrip !== null) {
            roundTrips.push(roundTrip);
        }
    }

    onStep('stability');
    let lost = 0;
    for (let i = 0; i < stabilitySeconds; i++) {
        const sentAt = probe.now();
        if ((await probe.roundTrip(pingInterval)) === null) {
            lost++;
        }
        // One a second, however long it took: a lost connection fails at once.
        await probe.sleep(Math.max(0, pingInterval - (probe.now() - sentAt)));
    }

    onStep('download');
    const throughput = await probe.download(downloadTimeout);

    return {
        transport: check.transport,
        sameSubnet: check.sameSubnet,
        roundTrips: summarizeRoundTrips(roundTrips),
        pings: stabilitySeconds,
        lost,
        reconnections: probe.reconnections - reconnectionsBefore,
        throughput,
    };
}

/** Measures through `connection`, to create before it starts so as not to miss its connection. */
export function createProbe(connection: GameConnection): NetworkProbe {
    let connected = false;
    let reconnections = 0;
    connection.onConnected(() => {
        connected = true;
    });
    connection.onReconnecting(() => {
        connected = false;
        reconnections++;
    });

    const sleep = (ms: number) => new Promise<void>((resolve) => setTimeout(resolve, ms));
    const now = () => performance.now();

    return {
        get reconnections() {
            return reconnections;
        },
        async waitConnected(timeout) {
            const deadline = now() + timeout;
            while (!connected && now() < deadline) {
                await sleep(100);
            }
            return connected;
        },
        checkNetwork: () => connection.invoke('CheckNetwork'),
        async roundTrip(timeout) {
            const sentAt = now();
            try {
                const answered = await Promise.race([
                    connection.invoke('SyncClock').then(() => true),
                    sleep(timeout).then(() => false),
                ]);
                return answered ? now() - sentAt : null;
            } catch {
                return null;
            }
        },
        async download(timeout) {
            const abort = new AbortController();
            const timer = setTimeout(() => abort.abort(), timeout);
            const startedAt = now();
            try {
                const response = await fetch(downloadUrl, {
                    cache: 'no-store',
                    signal: abort.signal,
                });
                if (!response.ok) {
                    return null;
                }
                const bytes = (await response.arrayBuffer()).byteLength;
                const seconds = Math.max(now() - startedAt, 1) / 1_000;
                return (bytes * 8) / seconds / 1_000_000;
            } catch {
                return null;
            } finally {
                clearTimeout(timer);
            }
        },
        sleep,
        now,
    };
}
