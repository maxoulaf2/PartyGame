<script lang="ts">
    import { onMount } from 'svelte';
    import ConnectionIndicator from '../shared/components/ConnectionIndicator.svelte';
    import ViewBoundary from '../shared/components/ViewBoundary.svelte';
    import WaitingScreen from '../shared/components/WaitingScreen.svelte';
    import type { GameMasterSnapshot } from '../shared/contracts';
    import { gameMasterCodeKey, localCodeStorage } from '../shared/connection/codeStorage';
    import { watchBuild } from '../shared/connection/buildCheck';
    import { ClockSync } from '../shared/connection/clockSync.svelte';
    import { ConnectionStatus } from '../shared/connection/connectionStatus.svelte';
    import { createGameConnection } from '../shared/connection/gameHub';
    import { connectErrorReporting } from '../shared/errors/errorReporting';
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
        const stopErrors = connectErrorReporting(connection, game);
        const stopSession = session.start();
        return () => {
            stopStatus();
            stopClock();
            stopBuild();
            stopErrors();
            stopSession();
        };
    });
</script>

<!-- What fails to render outside the view of a round, which has its own protection, gives way to
     the waiting screen until the next snapshot. -->
{#snippet unavailable()}
    <WaitingScreen title={fr.app.name} message={fr.gm.consoleUnavailable} />
{/snippet}

<ViewBoundary shown={game.current} fallback={unavailable}>
    {#if session.access === 'granted' && game.current}
        <GameConsole snapshot={game.current} {session} {clock} interactive={status.interactive} />
    {:else if session.access === 'codeRequired'}
        <CodeForm {session} interactive={status.interactive} />
    {:else}
        <WaitingScreen title={fr.app.name} message={fr.gm.waiting} />
    {/if}
</ViewBoundary>

<ConnectionIndicator {status} />
