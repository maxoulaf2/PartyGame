<script lang="ts">
    import type { IncidentInbox } from '../shared/connection/incidentInbox.svelte';
    import { countText } from '../shared/i18n/countText';
    import { fr } from '../shared/i18n/fr';
    import { describeIncident, incidentRoundText, incidentTimeText } from './incidentText';

    interface Props {
        /** The incidents of the server. Marking them as read needs no connection: it stays here. */
        inbox: IncidentInbox;
    }

    let { inbox }: Props = $props();

    let open = $state(false);
</script>

<!-- Nothing at all until the first incident: the game master is only disturbed when there is
     something to know. -->
{#if inbox.incidents.length > 0}
    <div class="incidents">
        <button
            type="button"
            class="counter"
            class:unread={inbox.unread > 0}
            aria-expanded={open}
            aria-controls="incident-panel"
            onclick={() => (open = !open)}
        >
            {countText(fr.gm.incidents.counter, inbox.unread)}
        </button>
        {#if open}
            <section id="incident-panel" aria-labelledby="incident-title">
                <h2 id="incident-title">{fr.gm.incidents.title}</h2>
                <p class="hint">{fr.gm.incidents.hint}</p>
                <ul aria-label={fr.gm.incidents.listLabel}>
                    {#each inbox.incidents as incident (incident.id)}
                        <li>
                            <span class="message">{describeIncident(incident)}</span>
                            <span class="details">
                                {incidentTimeText(incident)} · {incidentRoundText(incident)}
                            </span>
                        </li>
                    {/each}
                </ul>
                <div class="actions">
                    <button
                        type="button"
                        disabled={inbox.unread === 0}
                        onclick={() => inbox.markAllRead()}
                    >
                        {fr.gm.incidents.markAllRead}
                    </button>
                    <button type="button" class="secondary" onclick={() => (open = false)}>
                        {fr.gm.incidents.close}
                    </button>
                </div>
            </section>
        {/if}
    </div>
{/if}

<style>
    /* The counter and the panel lay out in the header of the console: the counter next to the
       title, the panel on a line of its own under it. */
    .incidents {
        display: contents;
    }

    h2,
    p,
    ul {
        margin: 0;
    }

    h2 {
        font-size: 1.25rem;
    }

    section {
        display: flex;
        flex-direction: column;
        flex-basis: 100%;
        gap: var(--space-s);
        padding: var(--space-m);
        border-radius: var(--radius);
        background: var(--color-surface);
    }

    .hint,
    .details {
        color: var(--color-text-muted);
        font-size: 0.875rem;
    }

    ul {
        display: flex;
        flex-direction: column;
        gap: var(--space-s);
        padding: 0;
        list-style: none;
    }

    li {
        display: flex;
        flex-direction: column;
        gap: 0.125rem;
        padding-left: var(--space-s);
        border-left: 3px solid var(--color-text-muted);
    }

    .details {
        overflow-wrap: anywhere;
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

    .secondary {
        border-color: var(--color-text-muted);
        color: var(--color-text);
    }

    /* Discreet once read: a quiet link to the list rather than a call to act. */
    .counter {
        border-color: transparent;
        color: var(--color-text-muted);
        font-size: 0.875rem;
        font-weight: 400;
    }

    .counter.unread {
        border-color: var(--color-accent);
        color: var(--color-accent);
        font-weight: 700;
    }
</style>
