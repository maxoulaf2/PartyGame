<script lang="ts">
    import { onMount } from 'svelte';
    import ConnectionIndicator from '../shared/components/ConnectionIndicator.svelte';
    import WaitingScreen from '../shared/components/WaitingScreen.svelte';
    import type { DisplaySnapshot } from '../shared/contracts';
    import { ClockSync } from '../shared/connection/clockSync.svelte';
    import { ConnectionStatus } from '../shared/connection/connectionStatus.svelte';
    import { connectDisplay } from '../shared/connection/displayConnection';
    import { createGameConnection } from '../shared/connection/gameHub';
    import { SnapshotStore } from '../shared/connection/snapshotStore.svelte';
    import { fr } from '../shared/i18n/fr';
    import LobbyScreen from './LobbyScreen.svelte';

    const game = new SnapshotStore<DisplaySnapshot>();
    const connection = createGameConnection();
    const clock = new ClockSync(connection);
    // The TV screen has nothing to wait for but its snapshot, marked stale while disconnected.
    const status = new ConnectionStatus(() => game.fresh);

    onMount(() => {
        const stopStatus = status.start();
        // Before the connection starts, so as not to miss the first one.
        const stopClock = clock.start();
        const disconnect = connectDisplay(game, connection);
        return () => {
            stopStatus();
            stopClock();
            disconnect();
        };
    });
</script>

{#if game.current}
    <LobbyScreen snapshot={game.current} />
{:else}
    <!-- Never an empty screen: until the server first answers, the TV screen waits neutrally. -->
    <WaitingScreen title={fr.app.name} message={fr.display.waiting} />
{/if}

<ConnectionIndicator {status} tv />
