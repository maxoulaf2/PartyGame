/**
 * How long a page may stay out of sync with the server before it says so: shorter outages, such as
 * a phone switching between access points, go unnoticed.
 */
export const reconnectingNoticeDelay = 3_000;

/**
 * Where a page stands with the server:
 * - `connected`: the connection is up and the page shows the state of the server;
 * - `interrupted`: the connection was lost, or is back but the page has not received a fresh
 *   snapshot yet, for less than `reconnectingNoticeDelay`: interactions are locked, silently;
 * - `reconnecting`: the same for longer: a discreet notice says so, interactions stay locked.
 */
export type ConnectionState = 'connected' | 'interrupted' | 'reconnecting';

/**
 * The connection state of a page, derived from whether it is synchronized with the server. Views
 * lock their interactive elements from `interactive` alone, and `ConnectionIndicator` shows the
 * notice: neither holds any logic of its own.
 */
export class ConnectionStatus {
    readonly #isSynchronized: () => boolean;
    // A derived boolean only notifies when it flips: the delay of the notice is not restarted by
    // whatever else changes during an outage, such as a second lost connection.
    readonly #synchronized = $derived.by(() => this.#isSynchronized());
    #overdue = $state(false);

    /**
     * @param isSynchronized Whether the page is connected and shows the state of the server since
     * the connection was last established; read reactively. A page starts out of sync: the first
     * connection is waited for like any other.
     */
    constructor(isSynchronized: () => boolean) {
        this.#isSynchronized = isSynchronized;
    }

    get state(): ConnectionState {
        if (this.#synchronized) {
            return 'connected';
        }
        return this.#overdue ? 'reconnecting' : 'interrupted';
    }

    /** Whether the page may let the user act: buttons and fields are disabled otherwise. */
    get interactive(): boolean {
        return this.#synchronized;
    }

    /** Starts measuring the outages. Returns a function that stops. */
    start(): () => void {
        return $effect.root(() => {
            $effect(() => {
                if (this.#synchronized) {
                    this.#overdue = false;
                    return;
                }
                // A timer rather than the wall clock, which the user may set back or forth.
                const timer = setTimeout(() => {
                    this.#overdue = true;
                }, reconnectingNoticeDelay);
                return () => clearTimeout(timer);
            });
        });
    }
}
