<script lang="ts">
    import type { GameMasterRoundIntent, GameMasterSnapshot, RoundId } from '../shared/contracts';
    import type { ServerClock } from '../shared/connection/clockSync.svelte';
    import type { GameMasterSession } from '../shared/connection/gameMasterSession.svelte';
    import ViewBoundary from '../shared/components/ViewBoundary.svelte';
    import { selectGameScreen } from '../shared/gameScreen';
    import { fill, roundText } from '../shared/i18n/fill';
    import { fr } from '../shared/i18n/fr';
    import { findGameMasterView } from '../modes/registry';
    import RankingList from './RankingList.svelte';

    interface Props {
        snapshot: GameMasterSnapshot;
        session: GameMasterSession;
        /** The clock of the server, for the countdowns of the rounds. */
        clock: ServerClock;
        /** Whether the page is synchronized with the server: everything is disabled otherwise. */
        interactive: boolean;
    }

    let { snapshot, session, clock, interactive }: Props = $props();

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
        <!-- The rest of the console keeps working while the view fails: the players, the incidents,
             and skipping the round if it is offered. -->
        <ViewBoundary shown={snapshot}>
            <!-- A new round starts its view afresh: nothing of the previous one lingers. -->
            {#key screen.round.roundId}
                <ModeView view={screen.view} round={screen.round} {clock} {interactive} {send} />
            {/key}

            {#snippet fallback()}
                <p class="unavailable" role="status">{fr.gm.roundViewUnavailable}</p>
            {/snippet}
        </ViewBoundary>
    {:else if screen.kind === 'betweenRounds'}
        {@const round = screen.round}
        <p class="progress">{roundText(fr.game.roundEnded, round)}</p>
        <h2>{round.title}</h2>
        {#if snapshot.roundSkipped}
            <p class="skipped">{fr.gm.skipRound.skipped}</p>
        {/if}
        {#if snapshot.nextRoundTitle !== null}
            <p class="upcoming">
                {fill(fr.gm.nextRound.upcoming, {
                    number: round.number + 1,
                    count: round.count,
                    title: snapshot.nextRoundTitle,
                })}
            </p>
        {/if}
        <button
            type="button"
            disabled={!interactive || sending}
            aria-describedby="next-round-hint"
            onclick={() => nextRound(round.roundId)}
        >
            {fr.gm.nextRound.action}
        </button>
        <p id="next-round-hint" class="hint">{fr.gm.nextRound.hint}</p>
        <!-- After the action: with many players, a long ranking would push it out of sight. -->
        <h3>{fill(fr.game.rankingAfter, { number: round.number })}</h3>
        <RankingList ranking={snapshot.ranking} />
    {:else if screen.kind === 'finished'}
        <!-- Nothing left to start: a new game needs the server to restart. -->
        <h2>{fr.game.finished}</h2>
        {#if snapshot.roundSkipped}
            <p class="skipped">{fr.gm.skipRound.skipped}</p>
        {/if}
        <h3>{fr.game.finalRanking}</h3>
        <RankingList ranking={snapshot.ranking} />
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
    h3,
    p {
        margin: 0;
    }

    h3 {
        margin-top: var(--space-s);
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

    .skipped {
        color: var(--color-text-muted);
    }

    .unavailable {
        padding: var(--space-m) 0;
        color: var(--color-text-muted);
        font-weight: 700;
    }

    .upcoming {
        font-weight: 700;
        overflow-wrap: anywhere;
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
