import type { ServerClock } from './connection/clockSync.svelte';

/**
 * What the buzzer of a phone shows, as the view of a mode decides it from the snapshot:
 * - `closed`: nothing to buzz on yet;
 * - `open`: the first to press may answer;
 * - `sent`: pressed, the server has not decided yet;
 * - `won`: this player answers;
 * - `lost`: another player answers;
 * - `blocked`: this player may no longer buzz on this question.
 */
export type BuzzerState = 'closed' | 'open' | 'sent' | 'won' | 'lost' | 'blocked';

/**
 * The presses of a buzzer: a single buzz per opening, however many times or with however many
 * fingers the player presses, timed when the finger touched the screen and not when it is sent.
 */
export class BuzzerPresses {
    readonly #clock: Pick<ServerClock, 'synchronized' | 'toServerTime'>;
    readonly #buzz: (pressedAt: number) => void;
    // The opening last buzzed on: after a reload, the snapshot or the pending intents of the mode
    // tell instead that the buzz is sent.
    #pressed = $state<string | null>(null);

    /**
     * @param buzz Sends the buzz, with the time of the press on the clock of the server, in
     * milliseconds since the Unix epoch.
     */
    constructor(
        clock: Pick<ServerClock, 'synchronized' | 'toServerTime'>,
        buzz: (pressedAt: number) => void,
    ) {
        this.#clock = clock;
        this.#buzz = buzz;
    }

    /**
     * Whether a press may buzz now: the buzzer is open, the page shows the state of the server,
     * and its clock is synchronized, without which the time of the press would mean nothing.
     */
    enabled(state: BuzzerState, opening: string, interactive: boolean): boolean {
        return this.shown(state, opening) === 'open' && interactive && this.#clock.synchronized;
    }

    /** What the buzzer shows: sent at once once pressed, until the snapshot tells otherwise. */
    shown(state: BuzzerState, opening: string): BuzzerState {
        return state === 'open' && this.#pressed === opening ? 'sent' : state;
    }

    /**
     * A finger touched the buzzer at `timestamp`, a `performance.now()` time. Returns whether it
     * buzzed.
     */
    press(state: BuzzerState, opening: string, interactive: boolean, timestamp: number): boolean {
        if (!this.enabled(state, opening, interactive)) {
            return false;
        }
        this.#pressed = opening;
        this.#buzz(this.#clock.toServerTime(timestamp));
        return true;
    }
}
