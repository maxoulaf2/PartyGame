<script lang="ts">
    import ConfirmDialog from '../shared/components/ConfirmDialog.svelte';
    import type { GameMasterSnapshot } from '../shared/contracts';
    import type { GameMasterSession } from '../shared/connection/gameMasterSession.svelte';
    import { countText } from '../shared/i18n/countText';
    import { fr } from '../shared/i18n/fr';

    interface Props {
        snapshot: GameMasterSnapshot;
        session: GameMasterSession;
        /** Whether the page is synchronized with the server: everything is disabled otherwise. */
        interactive: boolean;
    }

    let { snapshot, session, interactive }: Props = $props();

    let confirming = $state(false);
    let sending = $state(false);
    let failed = $state(false);

    const playerCount = $derived(snapshot.players.length);
    const enoughPlayers = $derived(playerCount >= snapshot.minimumPlayerCount);
    const packChosen = $derived(snapshot.selectedPackId !== null);
    const canStart = $derived(interactive && !sending && enoughPlayers && packChosen);
    // Every reason why the game cannot start yet, read with the button.
    const hints = $derived(
        [packChosen ? null : 'start-pack-hint', enoughPlayers ? null : 'start-players-hint'].filter(
            (id) => id !== null,
        ),
    );

    async function start() {
        if (!canStart) {
            return;
        }
        sending = true;
        const outcome = await session.startGame();
        sending = false;
        confirming = false;
        // Too few players, no pack or already started: the snapshot shows it. A lost connection is for the
        // connection indicator to show.
        failed = outcome === 'StartFailed';
    }
</script>

{#if snapshot.phase === 'Lobby'}
    <section class="start">
        <button
            type="button"
            disabled={!canStart}
            aria-describedby={hints.length > 0 ? hints.join(' ') : undefined}
            onclick={() => {
                failed = false;
                confirming = true;
            }}
        >
            {fr.gm.start.action}
        </button>
        {#if !packChosen}
            <p id="start-pack-hint" class="hint">{fr.gm.start.packRequired}</p>
        {/if}
        {#if !enoughPlayers}
            <p id="start-players-hint" class="hint">
                {countText(fr.gm.start.minimumPlayers, snapshot.minimumPlayerCount)}
            </p>
        {/if}
        {#if failed && hints.length === 0}
            <p class="problem" role="alert">{fr.gm.start.failed}</p>
        {/if}
    </section>
    {#if confirming}
        <ConfirmDialog
            title={fr.gm.start.confirmTitle}
            message={countText(fr.gm.start.confirmMessage, playerCount)}
            confirmLabel={fr.gm.start.confirm}
            cancelLabel={fr.gm.start.cancel}
            confirmDisabled={!canStart}
            onconfirm={start}
            oncancel={() => (confirming = false)}
        />
    {/if}
{:else}
    <p class="started" role="status">{fr.gm.started}</p>
{/if}

<style>
    .start {
        display: flex;
        flex-direction: column;
        gap: var(--space-s);
    }

    p {
        margin: 0;
    }

    button {
        min-height: var(--touch-target-min);
        padding: 0 var(--space-l);
        border: none;
        border-radius: var(--radius);
        background: var(--color-accent);
        color: var(--color-bg);
        font: inherit;
        font-size: 1.125rem;
        font-weight: 700;
        cursor: pointer;
        touch-action: manipulation;
    }

    button:disabled {
        opacity: 0.4;
        cursor: default;
    }

    .hint {
        color: var(--color-text-muted);
    }

    .problem {
        color: var(--color-accent);
        font-weight: 700;
    }

    .started {
        align-self: flex-start;
        padding: var(--space-s) var(--space-m);
        border: 2px solid var(--color-accent);
        border-radius: var(--radius);
        color: var(--color-accent);
        font-weight: 700;
    }
</style>
