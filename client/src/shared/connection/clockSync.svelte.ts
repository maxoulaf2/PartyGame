import { estimateClock, type ClockEstimate, type ClockSample } from './clockEstimate';
import type { GameConnection } from './gameHub';

/** Round trips in a burst (decision 2 of E05). */
export const clockSyncBurstSize = 8;
/** Pause between two round trips of a burst, not to flood the Wi-Fi. */
export const clockSyncSpacing = 50;
/** Time between two bursts while the connection stays up. */
export const clockSyncInterval = 60_000;
/**
 * Round trips a burst needs to replace the estimate. A lost round trip is ignored, but a burst
 * that lost most of them, or during which the connection dropped, keeps the previous estimate.
 */
export const clockSyncMinimumSamples = clockSyncBurstSize / 2;

/**
 * The local clock: `performance.now()` and its origin. Never the wall clock, which the user or the
 * network may set back or forth during a game.
 */
export interface LocalClock {
    /** Milliseconds since the Unix epoch when `now()` was 0. */
    readonly origin: number;
    /** Monotonic milliseconds since `origin`. */
    now(): number;
}

const performanceClock: LocalClock = {
    get origin() {
        return performance.timeOrigin;
    },
    now: () => performance.now(),
};

/** The clock of the server, as the views of the modes read it to count down. */
export interface ServerClock {
    /** The current time of the server, in milliseconds since the Unix epoch. */
    serverNow(): number;
}

/**
 * Keeps an estimate of the clock of the server, as NTP does: a burst of round trips, of which the
 * fastest count, at every connection and every minute. Countdowns, buzzes and synchronized sounds
 * convert their times with it.
 *
 * Before the first burst completes, conversions assume both clocks agree.
 */
export class ClockSync implements ServerClock {
    #estimate = $state.raw<ClockEstimate | null>(null);
    #synchronized = $state(false);

    readonly #connection: GameConnection;
    readonly #clock: LocalClock;

    #started = false;
    #stopped = false;
    #running = false;
    #pending = false;
    // Counts lost connections: a burst during which the connection dropped is not trusted.
    #losses = 0;
    #next: ReturnType<typeof setTimeout> | undefined;

    constructor(connection: GameConnection, clock: LocalClock = performanceClock) {
        this.#connection = connection;
        this.#clock = clock;
    }

    /**
     * Whether the estimate comes from a burst made on the current connection. It turns false when
     * the connection is lost: a phone asleep meanwhile may have paused its monotonic clock.
     */
    get synchronized(): boolean {
        return this.#synchronized;
    }

    /** What to add to a local time to get the time of the server, in milliseconds. */
    get offset(): number {
        return this.#estimate?.offset ?? 0;
    }

    /** The shortest round trip of the last burst, in milliseconds, or null before any. */
    get roundTrip(): number | null {
        return this.#estimate?.roundTrip ?? null;
    }

    /** The current time of the server, in milliseconds since the Unix epoch. */
    serverNow(): number {
        return this.toServerTime(this.#clock.now());
    }

    /** The time of the server, since the Unix epoch, at a local `performance.now()` timestamp. */
    toServerTime(timestamp: number): number {
        return this.#clock.origin + timestamp + this.offset;
    }

    /** The local `performance.now()` timestamp at a time of the server, since the Unix epoch. */
    toLocalTime(serverTime: number): number {
        return serverTime - this.offset - this.#clock.origin;
    }

    /**
     * Synchronizes at every connection of `connection`, then every minute. Call it before the
     * connection starts, so as not to miss the first one. Returns a function that stops.
     */
    start(): () => void {
        if (!this.#started) {
            this.#started = true;
            // Callbacks cannot be removed from the connection: stopping silences them instead.
            this.#connection.onConnected(() => this.#request());
            this.#connection.onReconnecting(() => {
                this.#losses++;
                if (!this.#stopped) {
                    this.#synchronized = false;
                }
            });
        }
        this.#stopped = false;
        return () => {
            this.#stopped = true;
            clearTimeout(this.#next);
        };
    }

    /** Runs a burst, or another one after the current burst when a connection came meanwhile. */
    #request(): void {
        if (this.#stopped) {
            return;
        }
        if (this.#running) {
            this.#pending = true;
            return;
        }
        void this.#run();
    }

    async #run(): Promise<void> {
        clearTimeout(this.#next);
        this.#running = true;
        try {
            do {
                this.#pending = false;
                await this.#burst();
            } while (this.#pending && !this.#stopped);
        } finally {
            this.#running = false;
        }
        if (!this.#stopped) {
            this.#next = setTimeout(() => this.#request(), clockSyncInterval);
        }
    }

    async #burst(): Promise<void> {
        const losses = this.#losses;
        const samples: ClockSample[] = [];
        for (let i = 0; i < clockSyncBurstSize && !this.#stopped; i++) {
            if (i > 0) {
                await new Promise((resolve) => setTimeout(resolve, clockSyncSpacing));
            }
            const sample = await this.#measure();
            if (sample !== null) {
                samples.push(sample);
            }
        }
        if (this.#stopped || this.#losses !== losses || samples.length < clockSyncMinimumSamples) {
            return;
        }
        this.#estimate = estimateClock(samples);
        this.#synchronized = true;
    }

    async #measure(): Promise<ClockSample | null> {
        const sentAt = this.#clock.origin + this.#clock.now();
        try {
            const { serverTime } = await this.#connection.invoke('SyncClock');
            return { sentAt, serverTime, receivedAt: this.#clock.origin + this.#clock.now() };
        } catch {
            // Connection lost meanwhile: this round trip is ignored.
            return null;
        }
    }
}
