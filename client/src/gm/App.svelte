<script lang="ts">
    import { onMount } from 'svelte';
    import ConnectionIndicator from '../shared/components/ConnectionIndicator.svelte';
    import WaitingScreen from '../shared/components/WaitingScreen.svelte';
    import type { GameMasterSnapshot } from '../shared/contracts';
    import { gameMasterCodeKey, localCodeStorage } from '../shared/connection/codeStorage';
    import { watchBuild } from '../shared/connection/buildCheck';
    import { ClockSync } from '../shared/connection/clockSync.svelte';
    import { ConnectionStatus } from '../shared/connection/connectionStatus.svelte';
    import { createGameConnection } from '../shared/connection/gameHub';
    import { GameMasterSession } from '../shared/connection/gameMasterSession.svelte';
    import { SnapshotStore } from '../shared/connection/snapshotStore.svelte';
    import { fr } from '../shared/i18n/fr';
    import CodeForm from './CodeForm.svelte';
    import GameConsole from './GameConsole.svelte';

    const game = new SnapshotStore<GameMasterSnapshot>();
    const connection = createGameConnection();
    const session = new GameMasterSession(game, localCodeStorage(gameMasterCodeKey), connection);
    const clock = new ClockSync(connection);
    const status = new ConnectionStatus(() => session.synchronized);

    onMount(() => {
        const stopStatus = status.start();
        // Before the session starts the connection, so as not to miss the first one, nor the welcome.
        const stopClock = clock.start();
        const stopBuild = watchBuild(connection);
        const stopSession = session.start();
        return () => {
            stopStatus();
            stopClock();
            stopBuild();
            stopSession();
        };
    });
</script>

{#if session.access === 'granted' && game.current}
    <GameConsole snapshot={game.current} {session} interactive={status.interactive} />
{:else if session.access === 'codeRequired'}
    <CodeForm {session} interactive={status.interactive} />
{:else}
    <WaitingScreen title={fr.app.name} message={fr.gm.waiting} />
{/if}

<ConnectionIndicator {status} />
