import type { DisplaySnapshot } from '../contracts';
import { createGameConnection, type GameConnection } from './gameHub';
import type { SnapshotStore } from './snapshotStore.svelte';

/**
 * Connects the page as the TV screen, which needs no secret, and keeps `store` up to date with
 * every snapshot the server sends: the current one right after the announcement, then one per
 * change. Returns a function that disconnects.
 */
export function connectDisplay(
    store: SnapshotStore<DisplaySnapshot>,
    connection: GameConnection = createGameConnection(),
): () => void {
    const unsubscribe = connection.on('ReceiveDisplaySnapshot', (snapshot) => {
        store.accept(snapshot);
    });

    connection
        .start()
        .then(() => connection.invoke('Announce', { role: 'Display', gameMasterCode: null }))
        // An unreachable server leaves the TV screen on its neutral display, never on an error.
        // Reconnecting is the job of E05.
        .catch(() => {});

    return () => {
        unsubscribe();
        connection.stop().catch(() => {});
    };
}
