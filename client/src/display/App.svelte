<script lang="ts">
    import { onMount } from 'svelte';
    import WaitingScreen from '../shared/components/WaitingScreen.svelte';
    import type { DisplaySnapshot } from '../shared/contracts';
    import { connectDisplay } from '../shared/connection/displayConnection';
    import { SnapshotStore } from '../shared/connection/snapshotStore.svelte';
    import { fr } from '../shared/i18n/fr';
    import LobbyScreen from './LobbyScreen.svelte';

    const game = new SnapshotStore<DisplaySnapshot>();

    onMount(() => connectDisplay(game));
</script>

{#if game.current}
    <LobbyScreen snapshot={game.current} />
{:else}
    <!-- Never an empty screen: until the server first answers, the TV screen waits neutrally. -->
    <WaitingScreen title={fr.app.name} message={fr.display.waiting} />
{/if}
