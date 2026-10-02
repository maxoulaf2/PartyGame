<script lang="ts">
    import { onMount } from 'svelte';
    import WaitingScreen from '../shared/components/WaitingScreen.svelte';
    import type { GameMasterSnapshot } from '../shared/contracts';
    import { gameMasterCodeKey, localCodeStorage } from '../shared/connection/codeStorage';
    import { createGameConnection } from '../shared/connection/gameHub';
    import { GameMasterSession } from '../shared/connection/gameMasterSession.svelte';
    import { SnapshotStore } from '../shared/connection/snapshotStore.svelte';
    import { fr } from '../shared/i18n/fr';
    import CodeForm from './CodeForm.svelte';
    import LobbyConsole from './LobbyConsole.svelte';

    const game = new SnapshotStore<GameMasterSnapshot>();
    const session = new GameMasterSession(
        game,
        localCodeStorage(gameMasterCodeKey),
        createGameConnection(),
    );

    onMount(() => session.start());
</script>

{#if session.access === 'granted' && game.current}
    <LobbyConsole snapshot={game.current} />
{:else if session.access === 'codeRequired'}
    <CodeForm {session} />
{:else}
    <WaitingScreen title={fr.app.name} message={fr.gm.waiting} />
{/if}
