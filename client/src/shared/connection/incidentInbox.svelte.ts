import type { Incident, IncidentList } from '../contracts';

/**
 * The incidents of the server, as the GM console shows them: every incident it keeps, and how many
 * occurred since the game master last marked them as read. The read mark lives on this console
 * only: two consoles each have their own.
 */
export class IncidentInbox {
    // Raw: a list is replaced as a whole, never modified, so it needs no deep reactivity.
    #list = $state.raw<IncidentList | null>(null);
    #readVersion = $state(0);
    // Whether the connection was lost since the last list: the next one is taken whatever its version.
    #stale = false;

    /** Every incident the server keeps, the one that happened last first. */
    get incidents(): readonly Incident[] {
        return this.#list?.incidents ?? [];
    }

    /**
     * How many incidents occurred since the game master last marked them as read, repetitions
     * included: the version of a list counts every occurrence.
     */
    get unread(): number {
        return Math.max(0, (this.#list?.version ?? 0) - this.#readVersion);
    }

    /**
     * Keeps `list` unless an older one: the list sent on announcement and one sent on a new
     * incident may arrive out of order. Returns whether it was kept.
     */
    accept(list: IncidentList): boolean {
        const current = this.#list;
        if (current !== null && !this.#stale && list.version <= current.version) {
            return false;
        }
        if (current !== null && list.version < current.version) {
            // Only a server that restarted counts from lower: none of its incidents was read.
            this.#readVersion = 0;
        }
        this.#stale = false;
        this.#list = list;
        return true;
    }

    /**
     * Takes the next list whatever its version, after a lost connection: the server may have
     * restarted meanwhile, and counted its incidents from 0 again.
     */
    markStale(): void {
        this.#stale = true;
    }

    /** Clears the counter until the next incident. The incidents stay listed. */
    markAllRead(): void {
        this.#readVersion = this.#list?.version ?? 0;
    }
}
