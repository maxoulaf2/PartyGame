<script lang="ts">
    import type {
        BuzzerAskQuestion,
        BuzzerGameMasterView,
        BuzzerJudge,
        BuzzerNextQuestion,
        BuzzerRevealAnswer,
        BuzzerShowQuestion,
    } from '../../shared/contracts';
    import { fill } from '../../shared/i18n/fill';
    import { fr } from '../../shared/i18n/fr';
    import type { GameMasterViewProps } from '../../shared/modeViews';

    type Intent =
        | BuzzerAskQuestion
        | BuzzerShowQuestion
        | BuzzerJudge
        | BuzzerRevealAnswer
        | BuzzerNextQuestion;

    let { view, round, interactive, send }: GameMasterViewProps<BuzzerGameMasterView, Intent> =
        $props();

    let sending = $state(false);

    const lastQuestion = $derived(view.questionNumber === view.questionCount);

    async function act(intent: Intent) {
        if (!interactive || sending) {
            return;
        }
        sending = true;
        // Each intent names its question, and a judgment the opening it judges: sent again, or by
        // a second console, it changes nothing.
        await send(intent);
        sending = false;
    }

    function step(type: Exclude<Intent['type'], 'buzzer.judge' | 'buzzer.askQuestion'>) {
        void act({ type, roundId: round.roundId, questionNumber: view.questionNumber });
    }

    // Kept hidden, the question is read out while the players may already buzz.
    function ask(showQuestion: boolean) {
        void act({
            type: 'buzzer.askQuestion',
            roundId: round.roundId,
            questionNumber: view.questionNumber,
            showQuestion,
        });
    }

    function judge(correct: boolean) {
        void act({
            type: 'buzzer.judge',
            roundId: round.roundId,
            questionNumber: view.questionNumber,
            opening: view.opening,
            correct,
        });
    }
</script>

<div class="buzzer">
    <p class="progress">
        {fill(fr.modes.buzzer.question, { number: view.questionNumber, count: view.questionCount })}
    </p>
    <!-- The whole question from the start, for the game master to read it out. -->
    <div class="question" class:hidden={!view.shown}>
        <h3>{view.text}</h3>
        {#if !view.shown}
            <span class="hidden-label">{fr.modes.buzzer.gm.hiddenOnDisplay}</span>
        {/if}
    </div>
    <p class="answer">{fill(fr.modes.buzzer.gm.answer, { answer: view.answer })}</p>
    {#if view.phase === 'Answering' && view.winner !== null}
        <p class="winner" role="status">
            {fill(fr.modes.buzzer.hasHand, { nickname: view.winner })}
        </p>
    {:else if view.phase === 'Open'}
        <p class="waiting" role="status">{fr.modes.buzzer.gm.waitingBuzz}</p>
    {:else if view.phase === 'Closed'}
        <p class="waiting" role="status">{fr.modes.buzzer.gm.allBlocked}</p>
    {:else if view.phase === 'Revealed'}
        <p class="winner" role="status">
            {view.foundBy !== null
                ? fill(fr.modes.buzzer.foundBy, { nickname: view.foundBy })
                : fr.modes.buzzer.nobodyFound}
        </p>
    {/if}
    <div class="actions">
        {#if view.phase === 'Ready'}
            <button type="button" disabled={!interactive || sending} onclick={() => ask(true)}>
                {fr.modes.buzzer.gm.askQuestion}
            </button>
            <button
                type="button"
                class="secondary"
                disabled={!interactive || sending}
                onclick={() => ask(false)}
            >
                {fr.modes.buzzer.gm.openHidden}
            </button>
        {:else if view.phase === 'Revealed'}
            <button
                type="button"
                disabled={!interactive || sending}
                onclick={() => step('buzzer.nextQuestion')}
            >
                {lastQuestion ? fr.modes.buzzer.gm.endRound : fr.modes.buzzer.gm.nextQuestion}
            </button>
        {:else}
            {#if !view.shown}
                <button
                    type="button"
                    disabled={!interactive || sending}
                    onclick={() => step('buzzer.showQuestion')}
                >
                    {fr.modes.buzzer.gm.showQuestion}
                </button>
            {/if}
            {#if view.phase === 'Answering'}
                <button
                    type="button"
                    disabled={!interactive || sending}
                    onclick={() => judge(true)}
                >
                    {fr.modes.buzzer.gm.correct}
                </button>
                <button
                    type="button"
                    class="wrong"
                    disabled={!interactive || sending}
                    onclick={() => judge(false)}
                >
                    {fr.modes.buzzer.gm.wrong}
                </button>
            {/if}
            <button
                type="button"
                class="secondary"
                disabled={!interactive || sending}
                onclick={() => step('buzzer.revealAnswer')}
            >
                {fr.modes.buzzer.gm.revealAnswer}
            </button>
        {/if}
    </div>
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

    /* Told apart by their text, and by their look rather than by a color alone. */
    button.wrong {
        border-style: dashed;
        background: transparent;
        color: var(--color-text);
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
