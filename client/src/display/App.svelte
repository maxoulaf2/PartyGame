<script lang="ts">
    import { onMount } from 'svelte';
    import ConnectionIndicator from '../shared/components/ConnectionIndicator.svelte';
    import WaitingScreen from '../shared/components/WaitingScreen.svelte';
    import type { DisplaySnapshot } from '../shared/contracts';
    import { watchBuild } from '../shared/connection/buildCheck';
    import { ClockSync } from '../shared/connection/clockSync.svelte';
    import { ConnectionStatus } from '../shared/connection/connectionStatus.svelte';
    import { connectDisplay } from '../shared/connection/displayConnection';
    import { createGameConnection } from '../shared/connection/gameHub';
    import { SnapshotStore } from '../shared/connection/snapshotStore.svelte';
    import { roundText } from '../shared/i18n/fill';
    import { fr } from '../shared/i18n/fr';
    import { selectGameScreen } from '../shared/gameScreen';
    import { findDisplayView } from '../modes/registry';
    import LobbyScreen, { type Notice } from './LobbyScreen.svelte';

    const game = new SnapshotStore<DisplaySnapshot>();
    const connection = createGameConnection();
    const clock = new ClockSync(connection);
    // The TV screen has nothing to wait for but its snapshot, marked stale while disconnected.
    const status = new ConnectionStatus(() => game.fresh);

    const screen = $derived(game.current && selectGameScreen(game.current, findDisplayView));
    // Outside a round, the lobby stays on screen with what is going on: its QR code still lets
    // late arrivals join, since registration stays open.
    const notice = $derived.by((): Notice | null => {
        switch (screen?.kind) {
            case 'betweenRounds':
                return {
                    headline: roundText(fr.game.roundEnded, screen.round),
                    detail: fr.display.betweenRounds,
                };
            case 'finished':
                return { headline: fr.game.finished, detail: fr.display.finished };
            case 'waiting':
                return { headline: fr.display.inProgress, detail: null };
            default:
                return null;
        }
    });

    onMount(() => {
        const stopStatus = status.start();
        // Before the connection starts, so as not to miss the first one, nor the welcome.
        const stopClock = clock.start();
        const stopBuild = watchBuild(connection);
        const disconnect = connectDisplay(game, connection);
        return () => {
            stopStatus();
            stopClock();
            stopBuild();
            disconnect();
        };
    });
</script>

{#if screen?.kind === 'round'}
    {@const ModeView = screen.component}
    <!-- A new round starts its view afresh: nothing of the previous one lingers. -->
    {#key screen.round.roundId}
        <ModeView view={screen.view} round={screen.round} {clock} />
    {/key}
{:else if game.current}
    <LobbyScreen snapshot={game.current} {notice} />
{:else}
    <!-- Never an empty screen: until the server first answers, the TV screen waits neutrally. -->
    <WaitingScreen title={fr.app.name} message={fr.display.waiting} />
{/if}

<ConnectionIndicator {status} tv />
