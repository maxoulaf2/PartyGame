<script lang="ts">
    import type { BuzzerAskQuestion, BuzzerGameMasterView } from '../../shared/contracts';
    import { fill } from '../../shared/i18n/fill';
    import { fr } from '../../shared/i18n/fr';
    import type { GameMasterViewProps } from '../../shared/modeViews';

    let {
        view,
        round,
        interactive,
        send,
    }: GameMasterViewProps<BuzzerGameMasterView, BuzzerAskQuestion> = $props();

    let sending = $state(false);

    async function askQuestion() {
        if (!interactive || sending) {
            return;
        }
        sending = true;
        // The intent names its question: sent again, or by a second console, it changes nothing.
        await send({
            type: 'buzzer.askQuestion',
            roundId: round.roundId,
            questionNumber: view.questionNumber,
        });
        sending = false;
    }
</script>

<div class="buzzer">
    <p class="progress">
        {fill(fr.modes.buzzer.question, { number: view.questionNumber, count: view.questionCount })}
    </p>
    <!-- The whole question from the start, for the game master to read it out. -->
    <div class="question" class:hidden={view.phase === 'Ready'}>
        <h3>{view.text}</h3>
        {#if view.phase === 'Ready'}
            <span class="hidden-label">{fr.modes.buzzer.gm.hiddenOnDisplay}</span>
        {/if}
    </div>
    <p class="answer">{fill(fr.modes.buzzer.gm.answer, { answer: view.answer })}</p>
    {#if view.phase === 'Ready'}
        <div class="actions">
            <button type="button" disabled={!interactive || sending} onclick={askQuestion}>
                {fr.modes.buzzer.gm.askQuestion}
            </button>
        </div>
    {:else if view.winner !== null}
        <p class="winner" role="status">
            {fill(fr.modes.buzzer.hasHand, { nickname: view.winner })}
        </p>
    {:else}
        <p class="waiting" role="status">{fr.modes.buzzer.gm.waitingBuzz}</p>
    {/if}
</div>

<style>
    .buzzer {
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
        overflow-wrap: anywhere;
    }

    .question {
        display: flex;
        flex-direction: column;
        gap: var(--space-s);
    }

    /* Not on the TV screen yet: dashed rather than faded alone, so that the text stays easy to
       read out. */
    .question.hidden {
        outline: 2px dashed var(--color-text-muted);
        outline-offset: 2px;
        border-radius: var(--radius);
    }

    .hidden-label {
        color: var(--color-text-muted);
        font-size: 0.875rem;
        font-style: italic;
    }

    .answer {
        color: var(--color-accent);
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

    button:disabled {
        opacity: 0.4;
        cursor: default;
    }
</style>
