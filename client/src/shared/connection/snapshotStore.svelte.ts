import type { GameId } from '../contracts';

/** What every snapshot carries, whatever its role: the game it belongs to and its version. */
export interface VersionedSnapshot {
    readonly gameId: GameId;
    readonly version: number;
}

/**
 * Whether `next` replaces `current`. A snapshot of another game always does: versions start over
 * when the server restarts a game. Within a game, only a strictly newer version does, since
 * snapshots of a game may arrive out of order (the one sent on announcement and a broadcast).
 */
export function supersedes(next: VersionedSnapshot, current: VersionedSnapshot | null): boolean {
    return current === null || next.gameId !== current.gameId || next.version > current.version;
}

/**
 * The last snapshot a page received, which it derives its whole display from. Pages never apply
 * game rules: they show what the server sent.
 */
export class SnapshotStore<T extends VersionedSnapshot> {
    // Raw: a snapshot is replaced as a whole, never modified, so it needs no deep reactivity.
    #current = $state.raw<T | null>(null);
    #fresh = $state(false);

    /** The snapshot to display, or null until the first one arrives. */
    get current(): T | null {
        return this.#current;
    }

    /**
     * Whether `current` reflects the server since the connection was last established. Interactive
     * elements stay disabled while it is false (US-E05-02).
     */
    get fresh(): boolean {
        return this.#fresh;
    }

    /** Keeps `snapshot` if it supersedes the current one. Returns whether it was kept. */
    accept(snapshot: T): boolean {
        if (supersedes(snapshot, this.#current)) {
            this.#current = snapshot;
            this.#fresh = true;
            return true;
        }
        // The same version again is what the server resends after a reconnection when nothing
        // changed meanwhile: it confirms the current snapshot.
        if (
            snapshot.gameId === this.#current?.gameId &&
            snapshot.version === this.#current.version
        ) {
            this.#fresh = true;
        }
        return false;
    }

    /**
     * Keeps showing the current snapshot, but marks it as possibly outdated until the next one
     * arrives, for instance after a disconnection.
     */
    markStale(): void {
        this.#fresh = false;
    }
}
