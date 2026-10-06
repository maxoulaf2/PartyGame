<script lang="ts">
    import type { GameMasterPlayer } from '../shared/contracts';
    import type { GameMasterSession } from '../shared/connection/gameMasterSession.svelte';
    import { fill } from '../shared/i18n/fill';
    import { fr } from '../shared/i18n/fr';
    import { adjustedScore, type ScoreEntryMode } from './scoreEntry';

    interface Props {
        player: GameMasterPlayer;
        session: GameMasterSession;
        /** Whether the page is synchronized with the server: everything is disabled otherwise. */
        interactive: boolean;
        /** Called once the adjustment is sent, or cancelled. */
        onclose: () => void;
    }

    let { player, session, interactive, onclose }: Props = $props();

    const modes: ScoreEntryMode[] = ['add', 'remove', 'set'];
    let mode = $state<ScoreEntryMode>('add');
    let amount = $state('');
    let sending = $state(false);
    let input: HTMLInputElement | undefined = $state();

    $effect(() => {
        input?.focus();
    });

    // From the score the snapshot shows, even if it changed since the form opened: the preview is
    // what the server applies, unless another console got there first.
    const newScore = $derived(adjustedScore(player.score, mode, amount));
    const negative = $derived(newScore !== null && newScore < 0);
    const canSubmit = $derived(interactive && !sending && newScore !== null && !negative);
    const fieldId = $derived(`score-${player.id}`);

    async function submit(event: SubmitEvent) {
        event.preventDefault();
        if (!canSubmit || newScore === null) {
            return;
        }
        sending = true;
        const outcome = await session.adjustScore(player.id, player.score, newScore);
        sending = false;
        // A rejected adjustment shows as the score of the next snapshot, like an accepted one.
        if (outcome === 'sent') {
            onclose();
        }
    }

    function cancel(event: KeyboardEvent) {
        if (event.key === 'Escape') {
            onclose();
        }
    }
</script>

<form onsubmit={submit} novalidate>
    <fieldset disabled={!interactive}>
        <legend>{fill(fr.gm.adjustScore.legend, { nickname: player.nickname })}</legend>
        <div class="modes">
            {#each modes as option (option)}
                <label class:selected={mode === option}>
                    <input type="radio" name="{fieldId}-mode" value={option} bind:group={mode} />
                    {fr.gm.adjustScore.modes[option]}
                </label>
            {/each}
        </div>
        <label for={fieldId}>{fr.gm.adjustScore.amount}</label>
        <input
            id={fieldId}
            bind:this={input}
            bind:value={amount}
            onkeydown={cancel}
            type="text"
            inputmode="numeric"
            autocomplete="off"
            enterkeyhint="done"
            aria-invalid={negative}
            aria-describedby="{fieldId}-preview"
        />
        <p id="{fieldId}-preview" class:problem={negative} role={negative ? 'alert' : undefined}>
            {#if negative}
                {fr.gm.adjustScore.negative}
            {:else if newScore !== null}
                {fill(fr.gm.adjustScore.preview, { old: player.score, new: newScore })}
            {/if}
        </p>
    </fieldset>
    <div class="actions">
        <button type="button" class="secondary" onclick={onclose}>{fr.gm.adjustScore.cancel}</button
        >
        <button type="submit" disabled={!canSubmit}>{fr.gm.adjustScore.submit}</button>
    </div>
</form>

<style>
    form,
    fieldset {
        display: flex;
        flex-direction: column;
        gap: var(--space-s);
    }

    fieldset {
        margin: 0;
        padding: 0;
        border: none;
    }

    legend,
    label {
        color: var(--color-text-muted);
        overflow-wrap: anywhere;
    }

    legend {
        margin-bottom: var(--space-s);
    }

    .modes {
        display: flex;
        gap: var(--space-s);
    }

    .modes label {
        display: flex;
        flex: 1;
        align-items: center;
        justify-content: center;
        min-height: var(--touch-target-min);
        border: 2px solid var(--color-text-muted);
        border-radius: var(--radius);
        color: var(--color-text);
        font-weight: 700;
        cursor: pointer;
        touch-action: manipulation;
    }

    /* The border and the weight tell the mode chosen, not the colour alone. */
    .modes label.selected {
        border-color: var(--color-accent);
        border-width: 3px;
        color: var(--color-accent);
    }

    .modes input {
        position: absolute;
        opacity: 0;
        pointer-events: none;
    }

    .modes label:has(input:focus-visible) {
        outline: 2px solid var(--color-accent);
        outline-offset: 2px;
    }

    input,
    button {
        min-height: var(--touch-target-min);
        border-radius: var(--radius);
        font: inherit;
    }

    input[type='text'] {
        padding: 0 var(--space-m);
        border: 2px solid var(--color-text-muted);
        background: var(--color-bg);
        color: var(--color-text);
        /* At least 16 px: Safari on iOS zooms into a smaller field when it gets the focus. */
        font-size: 1.25rem;
    }

    input[type='text']:focus-visible {
        border-color: var(--color-accent);
        outline: none;
    }

    p {
        min-height: 1.5em;
        margin: 0;
        font-weight: 700;
    }

    .problem {
        color: var(--color-accent);
    }

    .actions {
        display: flex;
        gap: var(--space-s);
    }

    button {
        flex: 1;
        padding: 0 var(--space-m);
        border: none;
        background: var(--color-accent);
        color: var(--color-bg);
        font-weight: 700;
        cursor: pointer;
        touch-action: manipulation;
    }

    .secondary {
        border: 2px solid var(--color-text-muted);
        background: transparent;
        color: var(--color-text);
    }

    button:disabled,
    fieldset:disabled {
        opacity: 0.4;
        cursor: default;
    }
</style>
