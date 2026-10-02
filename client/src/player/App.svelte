<script lang="ts">
    import { onMount } from 'svelte';
    import ConnectionIndicator from '../shared/components/ConnectionIndicator.svelte';
    import WaitingScreen from '../shared/components/WaitingScreen.svelte';
    import type { PlayerSnapshot } from '../shared/contracts';
    import {
        localCodeStorage,
        playerNicknameKey,
        playerTokenKey,
    } from '../shared/connection/codeStorage';
    import { watchBuild } from '../shared/connection/buildCheck';
    import { ClockSync } from '../shared/connection/clockSync.svelte';
    import { ConnectionStatus } from '../shared/connection/connectionStatus.svelte';
    import { createGameConnection } from '../shared/connection/gameHub';
    import { PlayerSession } from '../shared/connection/playerSession.svelte';
    import { SnapshotStore } from '../shared/connection/snapshotStore.svelte';
    import { fr } from '../shared/i18n/fr';
    import JoinForm from './JoinForm.svelte';
    import LobbyScreen from './LobbyScreen.svelte';

    const game = new SnapshotStore<PlayerSnapshot>();
    const connection = createGameConnection();
    const session = new PlayerSession(
        game,
        localCodeStorage(playerTokenKey),
        localCodeStorage(playerNicknameKey),
        connection,
    );
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

{#if session.joined && game.current}
    <LobbyScreen snapshot={game.current} />
{:else if session.status !== 'registering'}
    <!-- A phone that joined before waits for the server to recognize it, never on the form. -->
    <WaitingScreen title={fr.app.name} message={fr.player.resuming} />
{:else}
    <JoinForm {session} interactive={status.interactive} />
{/if}

<ConnectionIndicator {status} />
