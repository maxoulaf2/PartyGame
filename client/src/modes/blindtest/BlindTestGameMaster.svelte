<script lang="ts">
    import ConfirmDialog from '../../shared/components/ConfirmDialog.svelte';
    import type {
        BlindTestGameMasterView,
        BlindTestPlay,
        BlindTestSkipTrack,
    } from '../../shared/contracts';
    import { fill } from '../../shared/i18n/fill';
    import { fr } from '../../shared/i18n/fr';
    import type { GameMasterViewProps } from '../../shared/modeViews';

    type Intent = BlindTestPlay | BlindTestSkipTrack;

    let { view, round, interactive, send }: GameMasterViewProps<BlindTestGameMasterView, Intent> =
        $props();

    let sending = $state(false);
    // The track whose skip awaits confirmation: the dialog goes once the round moves on.
    let skipping = $state<number | null>(null);
    const confirmingSkip = $derived(skipping === view.trackNumber);

    async function act(type: Intent['type']) {
        if (!interactive || sending) {
            return;
        }
        sending = true;
        // Each intent names its track: sent again, or by a second console, it changes nothing.
        await send({ type, roundId: round.roundId, trackNumber: view.trackNumber });
        sending = false;
        skipping = null;
    }
</script>

<div class="blindtest">
    <p class="progress">
        {fill(fr.modes.blindtest.track, { number: view.trackNumber, count: view.trackCount })}
    </p>
    <h3>{fill(fr.modes.blindtest.gm.title, { title: view.title })}</h3>
    <p class="artist">
        {view.artist !== null
            ? fill(fr.modes.blindtest.gm.artist, { artist: view.artist })
            : fr.modes.blindtest.gm.noArtist}
    </p>
    {#if view.phase === 'Answering' && view.winner !== null}
        <p class="winner" role="status">
            {fill(fr.modes.blindtest.hasHand, { nickname: view.winner })}
        </p>
    {:else if view.phase === 'Listening'}
        <p class="waiting" role="status">{fr.modes.blindtest.gm.waitingBuzz}</p>
    {/if}
    <div class="actions">
        {#if view.phase === 'Ready'}
            <button
                type="button"
                disabled={!interactive || sending}
                onclick={() => act('blindtest.play')}
            >
                {fr.modes.blindtest.gm.play}
            </button>
        {/if}
        <button
            type="button"
            class="secondary"
            disabled={!interactive || sending}
            onclick={() => (skipping = view.trackNumber)}
        >
            {fr.modes.blindtest.gm.skipTrack}
        </button>
    </div>
</div>

{#if confirmingSkip}
    <ConfirmDialog
        title={fill(fr.modes.blindtest.gm.skipConfirm.title, { number: view.trackNumber })}
        message={view.trackNumber === view.trackCount
            ? fr.modes.blindtest.gm.skipConfirm.lastMessage
            : fr.modes.blindtest.gm.skipConfirm.message}
        confirmLabel={fr.modes.blindtest.gm.skipConfirm.confirm}
        cancelLabel={fr.modes.blindtest.gm.skipConfirm.cancel}
        confirmDisabled={!interactive || sending}
        onconfirm={() => act('blindtest.skipTrack')}
        oncancel={() => (skipping = null)}
    />
{/if}

<style>
    .blindtest {
        display: flex;
        flex-direction: column;
        gap: var(--space-s);
    }

    h3,
    p {
        margin: 0;
    }

    .progress {
        font-weight: 700;
    }

    h3 {
        font-size: 1.25rem;
        color: var(--color-accent);
        overflow-wrap: anywhere;
    }

    .artist {
        font-weight: 700;
        overflow-wrap: anywhere;
    }

    .winner {
        font-size: 1.5rem;
        font-weight: 800;
        overflow-wrap: anywhere;
    }

    .waiting {
        color: var(--color-text-muted);
    }

    .actions {
        display: flex;
        flex-wrap: wrap;
        gap: var(--space-s);
        margin-top: var(--space-s);
    }

    button {
        min-height: var(--touch-target-min);
        padding: 0 var(--space-l);
        border: 2px solid var(--color-accent);
        border-radius: var(--radius);
        background: var(--color-accent);
        color: var(--color-bg);
        font: inherit;
        font-size: 1.125rem;
        font-weight: 700;
        cursor: pointer;
        touch-action: manipulation;
    }

    button.secondary {
        background: transparent;
        color: var(--color-accent);
    }

    button:disabled {
        opacity: 0.4;
        cursor: default;
    }
</style>
