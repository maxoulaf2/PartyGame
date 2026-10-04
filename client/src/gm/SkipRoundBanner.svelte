<script lang="ts">
    import ConfirmDialog from '../shared/components/ConfirmDialog.svelte';
    import type { RoundId } from '../shared/contracts';
    import type { GameMasterSession } from '../shared/connection/gameMasterSession.svelte';
    import { fr } from '../shared/i18n/fr';

    interface Props {
        /** The round in progress, which keeps failing. */
        roundId: RoundId;
        session: GameMasterSession;
        /** Whether the page is synchronized with the server: everything is disabled otherwise. */
        interactive: boolean;
    }

    let { roundId, session, interactive }: Props = $props();

    let confirming = $state(false);
    let sending = $state(false);
    let failed = $state(false);

    const canSkip = $derived(interactive && !sending);

    async function skip() {
        if (!canSkip) {
            return;
        }
        sending = true;
        // Answered once the server handled it: the end of the round is in the snapshot by then,
        // and the banner is gone. Still here, the round was not skipped, or not yet: the request
        // is safe to repeat.
        const outcome = await session.skipRound(roundId);
        sending = false;
        confirming = false;
        failed = outcome === 'unreachable';
    }
</script>

<!-- Outside the view of the round: it stays offered even when that view cannot be shown. -->
<section class="banner" role="alert">
    <p class="problem">{fr.gm.skipRound.problem}</p>
    <p id="skip-round-hint" class="hint">{fr.gm.skipRound.hint}</p>
    <button
        type="button"
        disabled={!canSkip}
        aria-describedby="skip-round-hint"
        onclick={() => {
            failed = false;
            confirming = true;
        }}
    >
        {fr.gm.skipRound.action}
    </button>
    {#if failed}
        <p class="failed">{fr.gm.skipRound.failed}</p>
    {/if}
</section>
{#if confirming}
    <ConfirmDialog
        title={fr.gm.skipRound.confirmTitle}
        message={fr.gm.skipRound.confirmMessage}
        confirmLabel={fr.gm.skipRound.confirm}
        cancelLabel={fr.gm.skipRound.cancel}
        confirmDisabled={!canSkip}
        onconfirm={skip}
        oncancel={() => (confirming = false)}
    />
{/if}

<style>
    .banner {
        display: flex;
        flex-direction: column;
        gap: var(--space-s);
        padding: var(--space-m);
        border-radius: var(--radius);
        background: var(--color-accent);
        color: var(--color-bg);
    }

    p {
        margin: 0;
    }

    .problem {
        font-size: 1.125rem;
        font-weight: 700;
    }

    .failed {
        font-weight: 700;
    }

    button {
        align-self: flex-start;
        min-height: var(--touch-target-min);
        padding: 0 var(--space-l);
        border: 2px solid var(--color-bg);
        border-radius: var(--radius);
        background: var(--color-bg);
        color: var(--color-accent);
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
</style>
