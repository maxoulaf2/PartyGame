import type { DisplaySnapshot } from '../contracts';
import { createGameConnection, type GameConnection } from './gameHub';
import type { SnapshotStore } from './snapshotStore.svelte';

/**
 * Connects the page as the TV screen, which needs no secret, and keeps `store` up to date with
 * every snapshot the server sends: the current one right after the announcement, then one per
 * change. Announces again whenever the connection comes back. Returns a function that disconnects.
 */
export function connectDisplay(
    store: SnapshotStore<DisplaySnapshot>,
    connection: GameConnection = createGameConnection(),
): () => void {
    const unsubscribe = connection.on('ReceiveDisplaySnapshot', (snapshot) => {
        store.accept(snapshot);
    });

    // An unreachable server leaves the TV screen on its neutral display, never on an error.
    const announce = () =>
        connection.invoke('Announce', { role: 'Display', gameMasterCode: null }).catch(() => {});

    // The TV screen keeps showing the last snapshot, marked as possibly outdated (US-E05-02).
    connection.onReconnecting(() => store.markStale());
    connection.onReconnected(announce);
    connection
        .start()
        .then(announce)
        .catch(() => {});

    return () => {
        unsubscribe();
        connection.stop().catch(() => {});
    };
}
