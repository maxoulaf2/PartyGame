<script lang="ts">
    import ConnectionIcon from '../shared/components/ConnectionIcon.svelte';
    import type { GameMasterSnapshot, PlayerId } from '../shared/contracts';
    import type { ServerClock } from '../shared/connection/clockSync.svelte';
    import type { GameMasterSession } from '../shared/connection/gameMasterSession.svelte';
    import { countText } from '../shared/i18n/countText';
    import { fill } from '../shared/i18n/fill';
    import { fr } from '../shared/i18n/fr';
    import { pausedClock } from '../shared/pausedClock';
    import AddressControl from './AddressControl.svelte';
    import ConnectionQualityLine from './ConnectionQualityLine.svelte';
    import IncidentPanel from './IncidentPanel.svelte';
    import NetworkPanel from './NetworkPanel.svelte';
    import PackControl from './PackControl.svelte';
    import PreviewControl from './PreviewControl.svelte';
    import RenameForm from './RenameForm.svelte';
    import ResumeOffer from './ResumeOffer.svelte';
    import ReturnToLobbyControl from './ReturnToLobbyControl.svelte';
    import RoundControl from './RoundControl.svelte';
    import ScheduleControl from './ScheduleControl.svelte';
    import ScoreForm from './ScoreForm.svelte';
    import SkipRoundBanner from './SkipRoundBanner.svelte';
    import StartControl from './StartControl.svelte';

    interface Props {
        snapshot: GameMasterSnapshot;
        session: GameMasterSession;
        /** The clock of the server, for the countdowns of the rounds. */
        clock: ServerClock;
        /** Whether the page is synchronized with the server: everything is disabled otherwise. */
        interactive: boolean;
    }

    let { snapshot, session, clock, interactive }: Props = $props();

    // One rename at a time: the player whose form is open, followed by identifier through renames.
    let renaming = $state<PlayerId | null>(null);
    // One score adjustment at a time, likewise.
    let adjusting = $state<PlayerId | null>(null);

    const connectedCount = $derived(snapshot.players.filter((p) => p.isConnected).length);

    // How each phone and the TV screen reach the server, sent apart from the snapshots.
    const connections = $derived(session.network?.connections ?? []);
    const displayQuality = $derived(connections.find((c) => c.playerId === null) ?? null);
    const playerQualities = $derived(new Map(connections.map((c) => [c.playerId, c])));

    // Only a game in progress can be paused. While it is, the round stands still: its controls wait.
    const pausable = $derived(
        snapshot.phase === 'RoundIntro' ||
            snapshot.phase === 'Round' ||
            snapshot.phase === 'BetweenRounds',
    );
    const paused = $derived(snapshot.pausedAt !== null);
    const roundInteractive = $derived(interactive && !paused);

    // The pack the TV screen previews, from the catalog of the lobby.
    const previewed = $derived(
        snapshot.preview &&
            snapshot.packCatalog?.packs.find((p) => p.id === snapshot.preview?.packId),
    );
</script>

<main>
    <header>
        <h1>{fr.gm.consoleTitle}</h1>
        <IncidentPanel inbox={session.incidents} />
    </header>
    {#if snapshot.savedGame}
        <!-- Keyed: a confirmation open for one game found never discards another. -->
        {#key snapshot.savedGame.gameId}
            <ResumeOffer savedGame={snapshot.savedGame} {session} {interactive} />
        {/key}
    {:else}
        {@render console()}
    {/if}
</main>

{#snippet console()}
    <p class="counts">
        {countText(fr.gm.playersJoined, snapshot.players.length)}{#if snapshot.players.length > 0}
            · {countText(fr.gm.playersConnected, connectedCount)}{/if}
    </p>

    <!-- Offered for any round announced or in progress, as an alert when it keeps failing: a round
         announced may fail to start, and is skipped the same way. -->
    {#if (snapshot.phase === 'Round' || snapshot.phase === 'RoundIntro') && snapshot.round !== null}
        <!-- Keyed: a confirmation open for one round never skips another. -->
        {#key snapshot.round.roundId}
            <SkipRoundBanner
                roundId={snapshot.round.roundId}
                failing={session.incidents.isFailing(snapshot.round.roundId)}
                {session}
                interactive={roundInteractive}
            />
        {/key}
    {/if}

    {#if pausable}
        <!-- The snapshot tells whether the game is paused, whichever console asked. -->
        <button
            type="button"
            class="pause"
            aria-pressed={paused}
            disabled={!interactive}
            onclick={() => session.pauseGame(snapshot.gameId, !paused)}
        >
            {paused ? fr.gm.pause.resume : fr.gm.pause.pause}
        </button>
        {#if paused}
            <p class="paused" role="status">{fr.gm.pause.paused}</p>
        {/if}
    {/if}

    {#if snapshot.phase !== 'Lobby'}
        <RoundControl
            {snapshot}
            {session}
            clock={pausedClock(clock, snapshot.pausedAt)}
            interactive={roundInteractive}
        />
        {#if snapshot.schedule.length > 0}
            <!-- Changing the programme moves no round: it stays open during a pause. -->
            <ScheduleControl
                gameId={snapshot.gameId}
                schedule={snapshot.schedule}
                {session}
                interactive={interactive && pausable}
            />
        {/if}
        <!-- Keyed: a confirmation open for one game never ends another. -->
        {#key snapshot.gameId}
            <ReturnToLobbyControl
                gameId={snapshot.gameId}
                finished={snapshot.phase === 'Finished'}
                {session}
                {interactive}
            />
        {/key}
    {/if}

    <!-- Registration stays open once started: the address of the QR code may still change. -->
    <AddressControl {snapshot} {session} {interactive} />

    {#if snapshot.phase !== 'Lobby'}
        <!-- The lobby shows the QR code on the TV screen already. The snapshot tells what the TV shows,
             whichever console asked. -->
        <button
            type="button"
            class="join-code"
            aria-pressed={snapshot.joinCodeShown}
            disabled={!interactive || snapshot.joinAddress === null}
            onclick={() => session.showJoinCode(!snapshot.joinCodeShown)}
        >
            {snapshot.joinCodeShown ? fr.gm.joinCode.hide : fr.gm.joinCode.show}
        </button>
    {/if}

    <NetworkPanel
        joinAddress={snapshot.joinAddress}
        network={session.network}
        open={snapshot.phase === 'Lobby'}
    />

    {#if snapshot.preview && previewed}
        <PreviewControl
            preview={snapshot.preview}
            title={previewed.title ?? fr.gm.packs.untitled}
            rounds={previewed.rounds}
            {session}
            {interactive}
        />
    {/if}

    <PackControl {snapshot} {session} {interactive} />

    {#if snapshot.phase === 'Lobby'}
        <StartControl {snapshot} {session} {interactive} />
    {/if}

    {#if displayQuality !== null}
        <p class="display-quality">
            <span class="label">{fr.gm.network.display}</span>
            <ConnectionQualityLine quality={displayQuality} />
            {#if displayQuality.audioUnlocked === false}
                <span class="audio-locked">{fr.gm.network.displayAudioLocked}</span>
            {/if}
        </p>
    {/if}

    {#if snapshot.players.length > 0}
        <ul aria-label={fr.gm.playerListLabel}>
            {#each snapshot.players as player (player.id)}
                {@const quality = playerQualities.get(player.id)}
                <li class:disconnected={!player.isConnected}>
                    <div class="player">
                        <!-- Plain text interpolation: Svelte escapes it, so a nickname is never read as HTML. -->
                        <span class="nickname">{player.nickname}</span>
                        {#if snapshot.phase !== 'Lobby'}
                            <!-- Computed by the server, at every moment of the game. -->
                            <span class="score">{countText(fr.gm.score, player.score)}</span>
                        {/if}
                        {#if player.reconnectionCode !== null}
                            <!-- Read to the player who changed phone or browser, to join again. -->
                            <span class="code">
                                {fill(fr.gm.reconnectionCode, { code: player.reconnectionCode })}
                            </span>
                        {/if}
                        <span class="status">
                            <ConnectionIcon connected={player.isConnected} />
                            {player.isConnected ? fr.gm.connected : fr.gm.disconnected}
                        </span>
                        {#if quality !== undefined}
                            <ConnectionQualityLine {quality} />
                        {/if}
                        {#if renaming !== player.id}
                            <button
                                type="button"
                                disabled={!interactive}
                                aria-label={fr.gm.rename.actionFor.replace(
                                    '{nickname}',
                                    () => player.nickname,
                                )}
                                onclick={() => (renaming = player.id)}
                            >
                                {fr.gm.rename.action}
                            </button>
                        {/if}
                        {#if snapshot.phase !== 'Lobby' && adjusting !== player.id}
                            <button
                                type="button"
                                disabled={!interactive}
                                aria-label={fill(fr.gm.adjustScore.actionFor, {
                                    nickname: player.nickname,
                                })}
                                onclick={() => (adjusting = player.id)}
                            >
                                {fr.gm.adjustScore.action}
                            </button>
                        {/if}
                    </div>
                    {#if renaming === player.id}
                        <RenameForm
                            {player}
                            {session}
                            {interactive}
                            onclose={() => (renaming = null)}
                        />
                    {/if}
                    <!-- The scores mean nothing in the lobby: a return to it closes the form. -->
                    {#if adjusting === player.id && snapshot.phase !== 'Lobby'}
                        <ScoreForm
                            {player}
                            {session}
                            {interactive}
                            onclose={() => (adjusting = null)}
                        />
                    {/if}
                </li>
            {/each}
        </ul>
    {/if}
{/snippet}

<style>
    main {
        display: flex;
        flex-direction: column;
        gap: var(--space-m);
        max-width: 40rem;
        margin: 0 auto;
        padding: var(--space-l) var(--space-m);
    }

    h1,
    p {
        margin: 0;
    }

    header {
        display: flex;
        flex-wrap: wrap;
        align-items: flex-start;
        justify-content: space-between;
        gap: var(--space-s) var(--space-m);
    }

    h1 {
        color: var(--color-accent);
        font-size: var(--font-size-title);
    }

    .counts {
        color: var(--color-text-muted);
    }

    .display-quality {
        display: flex;
        flex-wrap: wrap;
        gap: var(--space-s);
        padding: 0 var(--space-m);
    }

    .display-quality .label {
        font-weight: 700;
    }

    .audio-locked {
        color: var(--color-accent);
    }

    ul {
        display: flex;
        flex-direction: column;
        gap: var(--space-s);
        margin: 0;
        padding: 0;
        list-style: none;
    }

    li {
        display: flex;
        flex-direction: column;
        gap: var(--space-s);
        padding: var(--space-s) var(--space-m);
        border-radius: var(--radius);
        background: var(--color-surface);
    }

    .player {
        display: flex;
        flex-wrap: wrap;
        align-items: center;
        gap: var(--space-s) var(--space-m);
        min-height: var(--touch-target-min);
    }

    .nickname {
        flex: 1 1 8rem;
        min-width: 0;
        font-size: 1.125rem;
        font-weight: 700;
        /* A long nickname wraps rather than being cut. */
        overflow-wrap: anywhere;
    }

    .score {
        font-weight: 700;
    }

    .code {
        color: var(--color-text-muted);
        font-family: ui-monospace, monospace;
        letter-spacing: 0.1em;
    }

    .status {
        display: inline-flex;
        align-items: center;
        gap: 0.4em;
        color: var(--color-text-muted);
        font-size: 0.875rem;
    }

    .disconnected .nickname {
        opacity: 0.6;
    }

    button {
        min-width: var(--touch-target-min);
        min-height: var(--touch-target-min);
        padding: 0 var(--space-m);
        border: 2px solid var(--color-accent);
        border-radius: var(--radius);
        background: transparent;
        color: var(--color-accent);
        font: inherit;
        font-weight: 700;
        cursor: pointer;
    }

    .paused {
        margin: 0;
        color: var(--color-accent);
        font-weight: 700;
    }

    .join-code,
    .pause {
        align-self: flex-start;
        touch-action: manipulation;
    }

    button:disabled {
        opacity: 0.4;
        cursor: default;
    }
</style>
