import { describe, expect, it } from 'vitest';
import type { DisplaySnapshot, GameId } from '../contracts';
import { SnapshotStore, supersedes } from './snapshotStore.svelte';

const gameA = 'a6f9619f-8b86-d011-b42d-00cf4fc964ff' as GameId;
const gameB = 'b6f9619f-8b86-d011-b42d-00cf4fc964ff' as GameId;

/** `host` tells apart snapshots of the same version that differ in content. */
function snapshot(version: number, gameId: GameId = gameA, host = 1): DisplaySnapshot {
    return {
        gameId,
        version,
        phase: 'Lobby',
        joinAddress: `10.0.0.${host}`,
        players: [],
        packTitle: null,
        round: null,
        roundView: null,
    };
}

describe('supersedes', () => {
    it('accepts any first snapshot', () => {
        expect(supersedes(snapshot(7), null)).toBe(true);
    });

    it('accepts only a strictly newer version of the same game', () => {
        expect(supersedes(snapshot(4), snapshot(3))).toBe(true);
        expect(supersedes(snapshot(3), snapshot(3))).toBe(false);
        expect(supersedes(snapshot(2), snapshot(3))).toBe(false);
    });

    it('accepts any version of another game', () => {
        expect(supersedes(snapshot(1, gameB), snapshot(9, gameA))).toBe(true);
    });
});

describe('SnapshotStore', () => {
    it('has no snapshot and is not fresh before the first one', () => {
        const store = new SnapshotStore<DisplaySnapshot>();

        expect(store.current).toBeNull();
        expect(store.fresh).toBe(false);
    });

    it('keeps the first snapshot whatever its version, and becomes fresh', () => {
        const store = new SnapshotStore<DisplaySnapshot>();

        expect(store.accept(snapshot(5))).toBe(true);

        expect(store.current).toEqual(snapshot(5));
        expect(store.fresh).toBe(true);
    });

    it('ignores an older or equal version of the same game', () => {
        const store = new SnapshotStore<DisplaySnapshot>();
        store.accept(snapshot(5, gameA, 2));

        expect(store.accept(snapshot(4, gameA, 1))).toBe(false);
        expect(store.accept(snapshot(5, gameA, 3))).toBe(false);

        expect(store.current).toEqual(snapshot(5, gameA, 2));
    });

    it('keeps snapshots received out of order at the newest', () => {
        const store = new SnapshotStore<DisplaySnapshot>();

        for (const version of [2, 4, 3, 1, 5]) {
            store.accept(snapshot(version));
        }

        expect(store.current?.version).toBe(5);
    });

    it('adopts a snapshot of another game even with a lower version', () => {
        const store = new SnapshotStore<DisplaySnapshot>();
        store.accept(snapshot(9, gameA));

        expect(store.accept(snapshot(1, gameB))).toBe(true);

        expect(store.current).toEqual(snapshot(1, gameB));
    });

    it('stays stale after a disconnection until a newer snapshot arrives', () => {
        const store = new SnapshotStore<DisplaySnapshot>();
        store.accept(snapshot(3));

        store.markStale();
        store.accept(snapshot(2));
        const freshAfterOlderVersion = store.fresh;
        store.accept(snapshot(4));

        expect(freshAfterOlderVersion).toBe(false);
        expect(store.current).toEqual(snapshot(4));
        expect(store.fresh).toBe(true);
    });

    it('becomes fresh again when the server confirms the current version', () => {
        const store = new SnapshotStore<DisplaySnapshot>();
        store.accept(snapshot(3, gameA, 2));
        store.markStale();

        expect(store.accept(snapshot(3, gameA, 2))).toBe(false);

        expect(store.fresh).toBe(true);
        expect(store.current).toEqual(snapshot(3, gameA, 2));
    });
});
