<script lang="ts">
    import ConfirmDialog from '../shared/components/ConfirmDialog.svelte';
    import type { GameId } from '../shared/contracts';
    import type { GameMasterSession } from '../shared/connection/gameMasterSession.svelte';
    import { fr } from '../shared/i18n/fr';

    interface Props {
        /** The current game, the one to end. */
        gameId: GameId;
        /** Whether the game is finished: nothing is lost then, so no confirmation is asked. */
        finished: boolean;
        session: GameMasterSession;
        /** Whether the page is synchronized with the server: everything is disabled otherwise. */
        interactive: boolean;
    }

    let { gameId, finished, session, interactive }: Props = $props();

    let confirming = $state(false);
    let sending = $state(false);
    let failed = $state(false);

    const canSend = $derived(interactive && !sending);

    async function returnToLobby() {
        if (!canSend) {
            return;
        }
        sending = true;
        // Answered once the server handled it: the lobby is in the snapshot by then, and this
        // control is gone. Naming the game makes the request safe to repeat.
        const outcome = await session.returnToLobby(gameId);
        sending = false;
        confirming = false;
        failed = outcome === 'unreachable';
    }
</script>

<section>
    <button
        type="button"
        disabled={!canSend}
        aria-describedby="return-to-lobby-hint"
        onclick={() => {
            failed = false;
            if (finished) {
                void returnToLobby();
            } else {
                confirming = true;
            }
        }}
    >
        {fr.gm.returnToLobby.action}
    </button>
    <p id="return-to-lobby-hint" class="hint">{fr.gm.returnToLobby.hint}</p>
    {#if failed}
        <p class="failed">{fr.gm.returnToLobby.failed}</p>
    {/if}
</section>
{#if confirming}
    <ConfirmDialog
        title={fr.gm.returnToLobby.confirmTitle}
        message={fr.gm.returnToLobby.confirmMessage}
        confirmLabel={fr.gm.returnToLobby.confirm}
        cancelLabel={fr.gm.returnToLobby.cancel}
        confirmDisabled={!canSend}
        onconfirm={returnToLobby}
        oncancel={() => (confirming = false)}
    />
{/if}

<style>
    section {
        display: flex;
        flex-direction: column;
        gap: var(--space-s);
    }

    p {
        margin: 0;
    }

    .hint {
        color: var(--color-text-muted);
    }

    .failed {
        color: var(--color-accent);
        font-weight: 700;
    }

    button {
        align-self: flex-start;
        min-height: var(--touch-target-min);
        padding: 0 var(--space-m);
        border: 2px solid var(--color-accent);
        border-radius: var(--radius);
        background: transparent;
        color: var(--color-accent);
        font: inherit;
        font-weight: 700;
        cursor: pointer;
        touch-action: manipulation;
    }

    button:disabled {
        opacity: 0.4;
        cursor: default;
    }
</style>
