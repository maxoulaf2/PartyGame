<script lang="ts">
    import ConfirmDialog from '../../shared/components/ConfirmDialog.svelte';
    import Countdown from '../../shared/components/Countdown.svelte';
    import type {
        OpenQuestionGameMasterView,
        OpenQuestionShowQuestion,
        OpenQuestionSkipQuestion,
    } from '../../shared/contracts';
    import { fill } from '../../shared/i18n/fill';
    import { fr } from '../../shared/i18n/fr';
    import type { GameMasterViewProps } from '../../shared/modeViews';

    type Intent = OpenQuestionShowQuestion | OpenQuestionSkipQuestion;

    let {
        view,
        round,
        clock,
        interactive,
        send,
    }: GameMasterViewProps<OpenQuestionGameMasterView, Intent> = $props();

    let sending = $state(false);
    // The question the game master asked to skip, until they confirm or cancel. The dialog goes away
    // on its own once the round moves on, for instance by a second console.
    let skipping = $state<number | null>(null);
    const confirmingSkip = $derived(skipping === view.questionNumber);

    const lastQuestion = $derived(view.questionNumber === view.questionCount);
    const answeredCount = $derived(view.answers.filter((answer) => answer.answer !== null).length);
    const allAnswered = $derived(view.answers.length > 0 && answeredCount === view.answers.length);

    async function step(type: Intent['type'], questionNumber = view.questionNumber) {
        if (!interactive || sending) {
            return;
        }
        sending = true;
        // Each intent names its question: sent again, or by a second console, it changes nothing.
        await send({ type, roundId: round.roundId, questionNumber });
        sending = false;
    }

    async function skip(questionNumber: number) {
        await step('openquestion.skipQuestion', questionNumber);
        skipping = null;
    }
</script>

<div class="openquestion">
    <div class="heading">
        <p class="progress">
            {fill(fr.modes.openquestion.question, {
                number: view.questionNumber,
                count: view.questionCount,
            })}
        </p>
        {#if view.answersCloseAt !== null}
            <p class="countdown">
                <Countdown
                    closeAt={view.answersCloseAt}
                    {clock}
                    label={fr.modes.openquestion.timeLeft}
                />
            </p>
        {:else if view.phase === 'Locked' && !allAnswered}
            <!-- Locked early once everybody answered: the line of answers already says so. -->
            <p class="time-up">{fr.modes.openquestion.timeUp}</p>
        {/if}
    </div>
    <!-- The console shows the question from the start, for the game master to read it out. -->
    <div class="question" class:hidden={!view.questionShown}>
        <h3>{view.text}</h3>
        {#if !view.questionShown}
            <span class="hidden-label">{fr.modes.openquestion.gm.hiddenOnDisplay}</span>
        {/if}
    </div>
    <p class="expected">
        {fill(fr.modes.openquestion.gm.expectedAnswer, { answer: view.expectedAnswer })}
    </p>
    {#if view.acceptedAnswers.length > 0}
        <p class="accepted">
            {fill(fr.modes.openquestion.gm.acceptedAnswers, {
                answers: view.acceptedAnswers.join(' · '),
            })}
        </p>
    {/if}

    {#if view.questionShown}
        <p class="answered" role="status">
            {fill(fr.modes.openquestion.answered, {
                answered: answeredCount,
                participants: view.answers.length,
            })}{#if allAnswered}
                · <strong>{fr.modes.openquestion.allAnswered}</strong>{/if}
        </p>
        <ul class="players" aria-label={fr.modes.openquestion.gm.answersLabel}>
            {#each view.answers as answer (answer.playerId)}
                <li>
                    <!-- Plain text interpolation: Svelte escapes nicknames and answers, never read as HTML. -->
                    <span class="nickname">{answer.nickname}</span>
                    {#if answer.answer !== null}
                        <span class="answer">{answer.answer}</span>
                    {:else}
                        <span class="none">
                            {view.phase === 'Answering'
                                ? fr.modes.openquestion.gm.waitingAnswer
                                : fr.modes.openquestion.gm.noAnswer}
                        </span>
                    {/if}
                </li>
            {/each}
        </ul>
    {/if}

    <div class="actions">
        {#if view.phase === 'Presentation'}
            <button
                type="button"
                disabled={!interactive || sending}
                onclick={() => step('openquestion.showQuestion')}
            >
                {fr.modes.openquestion.gm.showQuestion}
            </button>
        {/if}
        <button
            type="button"
            class="secondary"
            disabled={!interactive || sending}
            onclick={() => (skipping = view.questionNumber)}
        >
            {fr.modes.openquestion.gm.skipQuestion}
        </button>
    </div>
</div>
{#if confirmingSkip}
    <ConfirmDialog
        title={fill(fr.modes.openquestion.gm.skipConfirm.title, { number: view.questionNumber })}
        message={lastQuestion
            ? fr.modes.openquestion.gm.skipConfirm.lastMessage
            : fr.modes.openquestion.gm.skipConfirm.message}
        confirmLabel={fr.modes.openquestion.gm.skipConfirm.confirm}
        cancelLabel={fr.modes.openquestion.gm.skipConfirm.cancel}
        confirmDisabled={!interactive || sending}
        onconfirm={() => skip(view.questionNumber)}
        oncancel={() => (skipping = null)}
    />
{/if}

<style>
    .openquestion {
        display: flex;
        flex-direction: column;
        gap: var(--space-s);
    }

    h3,
    p {
        margin: 0;
    }

    .heading {
        display: flex;
        justify-content: space-between;
        align-items: center;
        gap: var(--space-m);
    }

    .progress,
    .expected {
        font-weight: 700;
    }

    .expected,
    .accepted,
    .answer {
        overflow-wrap: anywhere;
    }

    .countdown {
        color: var(--color-accent);
        font-size: 1.75rem;
    }

    .time-up {
        color: var(--color-accent);
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

    .accepted,
    .none,
    .answered {
        color: var(--color-text-muted);
    }

    .answered strong {
        color: var(--color-accent);
    }

    .players {
        display: flex;
        flex-direction: column;
        gap: var(--space-s);
        margin: 0;
        padding: 0;
        list-style: none;
    }

    .players li {
        display: flex;
        justify-content: space-between;
        align-items: center;
        gap: var(--space-m);
        padding: var(--space-s) var(--space-m);
        border-radius: var(--radius);
        background: var(--color-surface);
    }

    .nickname {
        min-width: 0;
        font-weight: 700;
        overflow-wrap: anywhere;
    }

    .answer {
        min-width: 0;
        text-align: right;
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

    button.secondary {
        background: transparent;
        color: var(--color-accent);
    }

    button:disabled {
        opacity: 0.4;
        cursor: default;
    }
</style>
