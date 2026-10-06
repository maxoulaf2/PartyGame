<script lang="ts">
    import type { GameMasterPackRound, GameMasterPreview } from '../shared/contracts';
    import type { GameMasterSession } from '../shared/connection/gameMasterSession.svelte';
    import type { IntentOutcome } from '../shared/connection/gameHub';
    import { fill } from '../shared/i18n/fill';
    import { fr } from '../shared/i18n/fr';
    import { describeMode } from './packProblemText';

    interface Props {
        preview: GameMasterPreview;
        /** The title of the pack previewed. */
        title: string;
        /** The rounds of the pack previewed, to choose one. */
        rounds: readonly GameMasterPackRound[];
        session: GameMasterSession;
        /** Whether the page is synchronized with the server: everything is disabled otherwise. */
        interactive: boolean;
    }

    let { preview, title, rounds, session, interactive }: Props = $props();

    let failed = $state(false);

    const round = $derived(preview.round);
    const step = $derived(preview.step);
    const isLast = $derived(step.number === step.count && round.number === round.count);

    // Each request names the step to show: the server ignores one that changes nothing.
    async function send(request: Promise<IntentOutcome>) {
        failed = (await request) === 'unreachable';
    }

    function show(roundNumber: number, stepNumber: number, playExcerpt = false) {
        void send(session.showPreviewStep(roundNumber, stepNumber, playExcerpt));
    }

    // The last step of a round leads to the first of the next one. Going back stops at the first
    // step of a round: the console does not know how many steps the previous one has.
    function next() {
        if (step.number < step.count) {
            show(round.number, step.number + 1);
        } else {
            show(round.number + 1, 1);
        }
    }
</script>

<section aria-labelledby="preview-title">
    <h2 id="preview-title">{fill(fr.gm.preview.title, { title })}</h2>
    <p class="position" aria-live="polite">
        {fill(fr.game.previewPosition, {
            number: round.number,
            count: round.count,
            step: step.number,
            steps: step.count,
        })}
    </p>
    <label>
        <span>{fr.gm.preview.roundLabel}</span>
        <select
            value={round.number}
            disabled={!interactive}
            onchange={(event) => show(Number(event.currentTarget.value), 1)}
        >
            {#each rounds as option, index (index)}
                <option value={index + 1}>
                    {fill(fr.gm.packs.round, {
                        title: option.title,
                        mode: describeMode(option.mode),
                    })}
                </option>
            {/each}
        </select>
    </label>
    <div class="actions">
        <button
            type="button"
            disabled={!interactive || step.number === 1}
            onclick={() => show(round.number, step.number - 1)}
        >
            {fr.gm.preview.previous}
        </button>
        <button type="button" disabled={!interactive || isLast} onclick={next}>
            {fr.gm.preview.next}
        </button>
        {#if preview.hasExcerpt}
            <button
                type="button"
                disabled={!interactive}
                onclick={() => show(round.number, step.number, true)}
            >
                {fr.gm.preview.playExcerpt}
            </button>
        {/if}
    </div>
    <button type="button" disabled={!interactive} onclick={() => send(session.stopPreview())}>
        {fr.gm.preview.stop}
    </button>
    {#if failed}
        <p class="failed" role="alert">{fr.gm.preview.failed}</p>
    {/if}
</section>

<style>
    section,
    label {
        display: flex;
        flex-direction: column;
        gap: var(--space-s);
    }

    section {
        padding: var(--space-s) var(--space-m);
        border: 2px solid var(--color-accent);
        border-radius: var(--radius);
    }

    h2,
    p {
        margin: 0;
    }

    h2 {
        font-size: 1.25rem;
        overflow-wrap: anywhere;
    }

    .position {
        font-weight: 700;
    }

    .actions {
        display: flex;
        flex-wrap: wrap;
        gap: var(--space-s);
    }

    select {
        min-height: var(--touch-target-min);
        font: inherit;
        touch-action: manipulation;
    }

    button {
        align-self: flex-start;
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

    .failed {
        color: var(--color-accent);
        font-weight: 700;
    }
</style>
