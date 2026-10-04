<script lang="ts">
    import { onMount } from 'svelte';
    import ConnectionIndicator from '../shared/components/ConnectionIndicator.svelte';
    import ViewBoundary from '../shared/components/ViewBoundary.svelte';
    import WaitingScreen from '../shared/components/WaitingScreen.svelte';
    import type { DisplaySnapshot } from '../shared/contracts';
    import { watchBuild } from '../shared/connection/buildCheck';
    import { ClockSync } from '../shared/connection/clockSync.svelte';
    import { ConnectionStatus } from '../shared/connection/connectionStatus.svelte';
    import { connectDisplay, reportMediaFailure } from '../shared/connection/displayConnection';
    import { createGameConnection } from '../shared/connection/gameHub';
    import { connectErrorReporting } from '../shared/errors/errorReporting';
    import { SnapshotStore } from '../shared/connection/snapshotStore.svelte';
    import { fr } from '../shared/i18n/fr';
    import { selectGameScreen } from '../shared/gameScreen';
    import { findDisplayView } from '../modes/registry';
    import FinalRankingScreen from './FinalRankingScreen.svelte';
    import LobbyScreen from './LobbyScreen.svelte';
    import RankingScreen from './RankingScreen.svelte';

    const game = new SnapshotStore<DisplaySnapshot>();
    const connection = createGameConnection();
    const clock = new ClockSync(connection);
    // The TV screen has nothing to wait for but its snapshot, marked stale while disconnected.
    const status = new ConnectionStatus(() => game.fresh);

    const screen = $derived(game.current && selectGameScreen(game.current, findDisplayView));
    // Outside a round and the rankings, the lobby stays on screen with what is going on: its QR code
    // still lets late arrivals join, since registration stays open.
    const notice = $derived(screen?.kind === 'waiting' ? fr.display.inProgress : null);
    const mediaFailed = (url: string) => reportMediaFailure(connection, url);

    onMount(() => {
        const stopStatus = status.start();
        // Before the connection starts, so as not to miss the first one, nor the welcome.
        const stopClock = clock.start();
        const stopBuild = watchBuild(connection);
        const stopErrors = connectErrorReporting(connection, game);
        const disconnect = connectDisplay(game, connection);
        return () => {
            stopStatus();
            stopClock();
            stopBuild();
            stopErrors();
            disconnect();
        };
    });
</script>

<!-- Never an empty screen: what fails to render gives way to the waiting screen until the next
     snapshot, the view of a round as the rest of the page. -->
{#snippet continuing()}
    <WaitingScreen title={fr.app.name} message={fr.display.continuing} />
{/snippet}

<ViewBoundary shown={game.current} fallback={continuing}>
    {#if screen?.kind === 'round'}
        {@const ModeView = screen.component}
        <ViewBoundary shown={game.current} fallback={continuing}>
            <!-- A new round starts its view afresh: nothing of the previous one lingers. -->
            {#key screen.round.roundId}
                <ModeView
                    view={screen.view}
                    round={screen.round}
                    {clock}
                    reportMediaFailure={mediaFailed}
                />
            {/key}
        </ViewBoundary>
    {:else if game.current && screen?.kind === 'betweenRounds'}
        <RankingScreen snapshot={game.current} round={screen.round} />
    {:else if game.current && screen?.kind === 'finished'}
        <FinalRankingScreen ranking={game.current.ranking} />
    {:else if game.current}
        <LobbyScreen snapshot={game.current} {notice} />
    {:else}
        <!-- Until the server first answers, the TV screen waits neutrally. -->
        <WaitingScreen title={fr.app.name} message={fr.display.waiting} />
    {/if}
</ViewBoundary>

<ConnectionIndicator {status} tv />
