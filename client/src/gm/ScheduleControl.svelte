<script lang="ts">
    import type { GameId, GameMasterScheduledRound, ScheduledRound } from '../shared/contracts';
    import type { GameMasterSession } from '../shared/connection/gameMasterSession.svelte';
    import { fill } from '../shared/i18n/fill';
    import { fr } from '../shared/i18n/fr';
    import { describeMode } from './packProblemText';
    import { editableOrder, moved, putBack, withdrawn } from './scheduleEdit';

    interface Props {
        gameId: GameId;
        schedule: readonly GameMasterScheduledRound[];
        session: GameMasterSession;
        /** Whether the programme may change: a game in progress, the page synchronized. */
        interactive: boolean;
    }

    let { gameId, schedule, session, interactive }: Props = $props();

    let sending = $state(false);

    const order = $derived(editableOrder(schedule));
    const enabled = $derived(interactive && !sending);

    // Answered once the server handled it: the new programme is in the snapshot by then. Rejected
    // because another console changed it first, the snapshot shows that programme instead.
    async function change(newOrder: ScheduledRound[] | null) {
        if (!enabled || newOrder === null) {
            return;
        }
        sending = true;
        await session.reorderRounds(gameId, order, newOrder);
        sending = false;
    }
</script>

<details class="schedule">
    <summary>{fr.gm.schedule.title}</summary>
    <ol>
        {#each schedule as round (round.roundIndex)}
            {@const strings = { title: round.title }}
            <li class:fixed={round.status !== 'Upcoming' && round.status !== 'Withdrawn'}>
                <span class="title">
                    {fill(fr.gm.packs.round, {
                        title: round.title,
                        mode: describeMode(round.mode),
                    })}
                </span>
                <span class="status">{fr.gm.schedule.status[round.status]}</span>
                {#if round.status === 'Upcoming'}
                    <span class="actions">
                        <button
                            type="button"
                            disabled={!enabled || moved(order, round.roundIndex, -1) === null}
                            aria-label={fill(fr.gm.schedule.upFor, strings)}
                            onclick={() => change(moved(order, round.roundIndex, -1))}
                        >
                            {fr.gm.schedule.up}
                        </button>
                        <button
                            type="button"
                            disabled={!enabled || moved(order, round.roundIndex, 1) === null}
                            aria-label={fill(fr.gm.schedule.downFor, strings)}
                            onclick={() => change(moved(order, round.roundIndex, 1))}
                        >
                            {fr.gm.schedule.down}
                        </button>
                        <button
                            type="button"
                            disabled={!enabled}
                            aria-label={fill(fr.gm.schedule.withdrawFor, strings)}
                            onclick={() => change(withdrawn(order, round.roundIndex))}
                        >
                            {fr.gm.schedule.withdraw}
                        </button>
                    </span>
                {:else if round.status === 'Withdrawn'}
                    <span class="actions">
                        <button
                            type="button"
                            disabled={!enabled}
                            aria-label={fill(fr.gm.schedule.putBackFor, strings)}
                            onclick={() => change(putBack(order, round.roundIndex))}
                        >
                            {fr.gm.schedule.putBack}
                        </button>
                    </span>
                {/if}
            </li>
        {/each}
    </ol>
</details>

<style>
    .schedule {
        padding: var(--space-m);
        border: 2px solid var(--color-accent);
        border-radius: var(--radius);
    }

    summary {
        display: flex;
        align-items: center;
        min-height: var(--touch-target-min);
        color: var(--color-accent);
        font-weight: 700;
        cursor: pointer;
        touch-action: manipulation;
        list-style: none;
    }

    /* The marker is drawn below, the same on every browser. */
    summary::-webkit-details-marker {
        display: none;
    }

    summary::before {
        content: '▸';
        margin-right: 0.5em;
    }

    details[open] summary::before {
        content: '▾';
    }

    ol {
        display: flex;
        flex-direction: column;
        gap: var(--space-s);
        margin: 0;
        padding-left: 1.25rem;
    }

    li {
        display: flex;
        flex-wrap: wrap;
        align-items: center;
        gap: var(--space-s);
    }

    .fixed {
        opacity: 0.7;
    }

    .title {
        overflow-wrap: anywhere;
    }

    .status {
        font-weight: 700;
    }

    .actions {
        display: flex;
        flex-wrap: wrap;
        gap: var(--space-s);
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
        touch-action: manipulation;
    }

    button:disabled {
        opacity: 0.4;
        cursor: default;
    }
</style>
