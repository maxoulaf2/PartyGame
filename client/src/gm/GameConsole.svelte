<script lang="ts">
    import ConnectionIcon from '../shared/components/ConnectionIcon.svelte';
    import type { GameMasterSnapshot, PlayerId } from '../shared/contracts';
    import type { ServerClock } from '../shared/connection/clockSync.svelte';
    import type { GameMasterSession } from '../shared/connection/gameMasterSession.svelte';
    import { countText } from '../shared/i18n/countText';
    import { fr } from '../shared/i18n/fr';
    import AddressControl from './AddressControl.svelte';
    import PackControl from './PackControl.svelte';
    import RenameForm from './RenameForm.svelte';
    import RoundControl from './RoundControl.svelte';
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

    const connectedCount = $derived(snapshot.players.filter((p) => p.isConnected).length);
</script>

<main>
    <h1>{fr.gm.consoleTitle}</h1>
    <p class="counts">
        {countText(fr.gm.playersJoined, snapshot.players.length)}{#if snapshot.players.length > 0}
            · {countText(fr.gm.playersConnected, connectedCount)}{/if}
    </p>

    {#if snapshot.phase !== 'Lobby'}
        <RoundControl {snapshot} {session} {clock} {interactive} />
    {/if}

    <!-- Registration stays open once started: the address of the QR code may still change. -->
    <AddressControl {snapshot} {session} {interactive} />

    <PackControl {snapshot} {session} {interactive} />

    {#if snapshot.phase === 'Lobby'}
        <StartControl {snapshot} {session} {interactive} />
    {/if}

    {#if snapshot.players.length > 0}
        <ul aria-label={fr.gm.playerListLabel}>
            {#each snapshot.players as player (player.id)}
                <li class:disconnected={!player.isConnected}>
                    <div class="player">
                        <!-- Plain text interpolation: Svelte escapes it, so a nickname is never read as HTML. -->
                        <span class="nickname">{player.nickname}</span>
                        <span class="status">
                            <ConnectionIcon connected={player.isConnected} />
                            {player.isConnected ? fr.gm.connected : fr.gm.disconnected}
                        </span>
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
                    </div>
                    {#if renaming === player.id}
                        <RenameForm
                            {player}
                            {session}
                            {interactive}
                            onclose={() => (renaming = null)}
                        />
                    {/if}
                </li>
            {/each}
        </ul>
    {/if}
</main>

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

    h1 {
        color: var(--color-accent);
        font-size: var(--font-size-title);
    }

    .counts {
        color: var(--color-text-muted);
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

    button:disabled {
        opacity: 0.4;
        cursor: default;
    }
</style>
