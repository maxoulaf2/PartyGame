<script lang="ts">
    import { onMount } from 'svelte';
    import ConnectionIndicator from '../shared/components/ConnectionIndicator.svelte';
    import ViewBoundary from '../shared/components/ViewBoundary.svelte';
    import WaitingScreen from '../shared/components/WaitingScreen.svelte';
    import type { DisplaySnapshot } from '../shared/contracts';
    import { watchBuild } from '../shared/connection/buildCheck';
    import { ClockSync } from '../shared/connection/clockSync.svelte';
    import { ConnectionStatus } from '../shared/connection/connectionStatus.svelte';
    import { reportRoundTrips } from '../shared/connection/roundTripReport.svelte';
    import {
        connectDisplay,
        reportAudio,
        reportMediaFailure,
    } from '../shared/connection/displayConnection';
    import { createGameConnection } from '../shared/connection/gameHub';
    import { connectErrorReporting } from '../shared/errors/errorReporting';
    import { SnapshotStore } from '../shared/connection/snapshotStore.svelte';
    import { fr } from '../shared/i18n/fr';
    import { selectGameScreen } from '../shared/gameScreen';
    import { findDisplayView } from '../modes/registry';
    import { unlockAudio } from './audioUnlock';
    import FinalRankingScreen from './FinalRankingScreen.svelte';
    import JoinCodeCorner from './JoinCodeCorner.svelte';
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
    // The lobby shows the QR code already, as does the screen it shows while waiting.
    const joinCodeAddress = $derived(
        game.current?.joinCodeShown &&
            (screen?.kind === 'round' ||
                screen?.kind === 'betweenRounds' ||
                screen?.kind === 'finished')
            ? game.current.joinAddress
            : null,
    );
    const mediaFailed = (url: string) => reportMediaFailure(connection, url);

    // Null until the browser tells: no button flashes on a screen whose audio plays already.
    let audioUnlocked = $state<boolean | null>(null);
    const announceAudio = () => {
        if (audioUnlocked !== null) {
            reportAudio(connection, audioUnlocked);
        }
    };
    async function checkAudio() {
        audioUnlocked = await unlockAudio();
        announceAudio();
    }

    onMount(() => {
        const stopStatus = status.start();
        // Before the connection starts, so as not to miss the first one, nor the welcome.
        const stopClock = clock.start();
        // For the game master console, which shows how well each phone and the TV screen reach the server.
        const stopRoundTrips = reportRoundTrips(connection, clock);
        const stopBuild = watchBuild(connection);
        const stopErrors = connectErrorReporting(connection, game);
        // Told again at every announcement: a restarted server forgets it.
        const disconnect = connectDisplay(game, connection, announceAudio);
        void checkAudio();
        return () => {
            stopStatus();
            stopClock();
            stopRoundTrips();
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
    {:else if game.current?.phase === 'ResumePending'}
        <!-- Restarted, the server waits for the game master: the game comes back as it was. -->
        <WaitingScreen title={fr.app.name} message={fr.display.resumePending} />
    {:else if game.current}
        <LobbyScreen snapshot={game.current} {notice} />
    {:else}
        <!-- Until the server first answers, the TV screen waits neutrally. -->
        <WaitingScreen title={fr.app.name} message={fr.display.waiting} />
    {/if}
</ViewBoundary>

{#if joinCodeAddress}
    <JoinCodeCorner joinAddress={joinCodeAddress} />
{/if}

{#if audioUnlocked === false}
    <!-- Over the current screen, which goes on underneath: a TV reloaded during the game needs a
         click again. It stays until the click unlocks the audio. -->
    <button type="button" class="start-audio" onclick={checkAudio}>{fr.display.startAudio}</button>
{/if}

<ConnectionIndicator {status} tv />

<style>
    /* Above the QR code of the lobby, within the 5% margin TVs may crop. */
    .start-audio {
        position: fixed;
        top: 5vh;
        left: 5vw;
        z-index: 2;
        width: 34vw;
        padding: var(--space-s) var(--space-m);
        border: none;
        border-radius: var(--radius);
        background: var(--color-accent);
        color: var(--color-bg);
        font: inherit;
        font-size: 3rem;
        font-weight: 700;
        cursor: pointer;
    }
</style>
