<script lang="ts">
    import { onMount } from 'svelte';
    import ConnectionIndicator from '../shared/components/ConnectionIndicator.svelte';
    import ViewBoundary from '../shared/components/ViewBoundary.svelte';
    import WaitingScreen from '../shared/components/WaitingScreen.svelte';
    import type { PlayerRoundIntent, PlayerSnapshot } from '../shared/contracts';
    import {
        localCodeStorage,
        playerNicknameKey,
        playerTokenKey,
    } from '../shared/connection/codeStorage';
    import { playerIntentsKey } from '../shared/connection/intentQueue.svelte';
    import { watchBuild } from '../shared/connection/buildCheck';
    import { ClockSync } from '../shared/connection/clockSync.svelte';
    import { ConnectionStatus } from '../shared/connection/connectionStatus.svelte';
    import { createGameConnection } from '../shared/connection/gameHub';
    import { connectErrorReporting } from '../shared/errors/errorReporting';
    import { PlayerSession } from '../shared/connection/playerSession.svelte';
    import { SnapshotStore } from '../shared/connection/snapshotStore.svelte';
    import { fr } from '../shared/i18n/fr';
    import { selectGameScreen } from '../shared/gameScreen';
    import { findPlayerView } from '../modes/registry';
    import FinalScreen from './FinalScreen.svelte';
    import JoinForm from './JoinForm.svelte';
    import LobbyScreen from './LobbyScreen.svelte';
    import RankingScreen from './RankingScreen.svelte';

    const game = new SnapshotStore<PlayerSnapshot>();
    const connection = createGameConnection();
    const session = new PlayerSession(
        game,
        localCodeStorage(playerTokenKey),
        localCodeStorage(playerNicknameKey),
        localCodeStorage(playerIntentsKey),
        connection,
    );
    const clock = new ClockSync(connection);
    const status = new ConnectionStatus(() => session.synchronized);

    const screen = $derived(game.current && selectGameScreen(game.current, findPlayerView));
    const send = (intent: PlayerRoundIntent) => session.sendRoundIntent(intent);
    // Those of the round shown only, which are intents of its mode.
    const pending = $derived(
        screen?.kind === 'round'
            ? session.pendingIntents.filter((intent) => intent.roundId === screen.round.roundId)
            : [],
    );

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

<!-- What fails to render gives way to the waiting screen, with nothing to tap, until the next
     snapshot: the view of a round as the rest of the page. -->
{#snippet inProgress()}
    <WaitingScreen title={fr.app.name} message={fr.player.inProgress} />
{/snippet}

<ViewBoundary shown={game.current} fallback={inProgress}>
    {#if session.joined && game.current && screen}
        {#if screen.kind === 'lobby'}
            <LobbyScreen snapshot={game.current} />
        {:else if screen.kind === 'round'}
            {@const ModeView = screen.component}
            <ViewBoundary shown={game.current} fallback={inProgress}>
                <!-- A new round starts its view afresh: nothing of the previous one lingers. -->
                {#key screen.round.roundId}
                    <ModeView
                        view={screen.view}
                        round={screen.round}
                        score={game.current.score}
                        {clock}
                        interactive={status.interactive}
                        {send}
                        {pending}
                    />
                {/key}
            </ViewBoundary>
        {:else if screen.kind === 'betweenRounds' && game.current.standing}
            <RankingScreen
                round={screen.round}
                standing={game.current.standing}
                score={game.current.score}
            />
        {:else if screen.kind === 'finished'}
            <FinalScreen standing={game.current.standing} score={game.current.score} />
        {:else}
            {@render inProgress()}
        {/if}
    {:else if session.status !== 'registering'}
        <!-- A phone that joined before waits for the server to recognize it, never on the form. -->
        <WaitingScreen title={fr.app.name} message={fr.player.resuming} />
    {:else}
        <JoinForm {session} interactive={status.interactive} />
    {/if}
</ViewBoundary>

<ConnectionIndicator {status} />
