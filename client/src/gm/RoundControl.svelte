<script lang="ts">
    import type { GameMasterRoundIntent, GameMasterSnapshot, RoundId } from '../shared/contracts';
    import type { GameMasterSession } from '../shared/connection/gameMasterSession.svelte';
    import { selectGameScreen } from '../shared/gameScreen';
    import { roundText } from '../shared/i18n/fill';
    import { fr } from '../shared/i18n/fr';
    import { findGameMasterView } from '../modes/registry';

    interface Props {
        snapshot: GameMasterSnapshot;
        session: GameMasterSession;
        /** Whether the page is synchronized with the server: everything is disabled otherwise. */
        interactive: boolean;
    }

    let { snapshot, session, interactive }: Props = $props();

    let sending = $state(false);

    const screen = $derived(selectGameScreen(snapshot, findGameMasterView));
    const send = (intent: GameMasterRoundIntent) => session.sendRoundIntent(intent);

    async function nextRound(afterRound: RoundId) {
        if (!interactive || sending) {
            return;
        }
        sending = true;
        // Answered once the server handled it: the new round is in the snapshot by then. A lost
        // connection is for the connection indicator to show, and the request is safe to repeat.
        await session.nextRound(afterRound);
        sending = false;
    }
</script>

<section class="round">
    {#if screen.kind === 'round'}
        {@const ModeView = screen.component}
        <p class="progress">{roundText(fr.game.round, screen.round)}</p>
        <h2>{screen.round.title}</h2>
        <!-- A new round starts its view afresh: nothing of the previous one lingers. -->
        {#key screen.round.roundId}
            <ModeView view={screen.view} round={screen.round} {interactive} {send} />
        {/key}
    {:else if screen.kind === 'betweenRounds'}
        {@const round = screen.round}
        <p class="progress">{roundText(fr.game.roundEnded, round)}</p>
        <h2>{round.title}</h2>
        <button
            type="button"
            disabled={!interactive || sending}
            aria-describedby="next-round-hint"
            onclick={() => nextRound(round.roundId)}
        >
            {fr.gm.nextRound.action}
        </button>
        <p id="next-round-hint" class="hint">{fr.gm.nextRound.hint}</p>
    {:else if screen.kind === 'finished'}
        <h2>{fr.game.finished}</h2>
    {:else}
        <p class="progress" role="status">{fr.gm.inProgress}</p>
    {/if}
</section>

<style>
    .round {
        display: flex;
        flex-direction: column;
        gap: var(--space-s);
        padding: var(--space-m);
        border: 2px solid var(--color-accent);
        border-radius: var(--radius);
    }

    h2,
    p {
        margin: 0;
    }

    h2 {
        color: var(--color-accent);
        overflow-wrap: anywhere;
    }

    .progress {
        font-weight: 700;
    }

    .hint {
        color: var(--color-text-muted);
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
</style>
