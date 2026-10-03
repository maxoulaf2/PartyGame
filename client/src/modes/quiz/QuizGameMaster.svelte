<script lang="ts">
    import ConfirmDialog from '../../shared/components/ConfirmDialog.svelte';
    import Countdown from '../../shared/components/Countdown.svelte';
    import type {
        QuizChoiceLetter,
        QuizGameMasterView,
        QuizLockAnswers,
        QuizNextQuestion,
        QuizOpenAnswers,
        QuizRevealAnswer,
        QuizSkipQuestion,
    } from '../../shared/contracts';
    import { countText } from '../../shared/i18n/countText';
    import { fill } from '../../shared/i18n/fill';
    import { fr } from '../../shared/i18n/fr';
    import type { GameMasterViewProps } from '../../shared/modeViews';
    import ChoiceMarker from './ChoiceMarker.svelte';
    import { choiceColor } from './choiceTheme';
    import CorrectMark from './CorrectMark.svelte';

    let { view, round, clock, interactive, send }: GameMasterViewProps<QuizGameMasterView, Intent> =
        $props();

    type Intent =
        QuizOpenAnswers | QuizLockAnswers | QuizRevealAnswer | QuizNextQuestion | QuizSkipQuestion;

    let sending = $state(false);
    // The question the game master asked to skip, until they confirm or cancel. The dialog goes away
    // on its own once the round moves on, for instance by a second console.
    let skipping = $state<number | null>(null);
    const confirmingSkip = $derived(skipping === view.questionNumber && view.phase !== 'Revealed');

    const lastQuestion = $derived(view.questionNumber === view.questionCount);

    const answeredCount = $derived(view.answers.filter((answer) => answer.choice !== null).length);
    const allAnswered = $derived(view.answers.length > 0 && answeredCount === view.answers.length);

    /** The nicknames of the players who chose `letter`, in order of arrival. */
    function chosenBy(letter: QuizChoiceLetter): string[] {
        return view.answers
            .filter((answer) => answer.choice === letter)
            .map((answer) => answer.nickname);
    }

    async function act(type: Intent['type'], questionNumber = view.questionNumber) {
        if (!interactive || sending) {
            return;
        }
        sending = true;
        // Each intent names its question: sent again, or by a second console, it changes nothing.
        // A lost connection is for the connection indicator to show.
        await send({ type, roundId: round.roundId, questionNumber });
        sending = false;
    }

    async function skip(questionNumber: number) {
        await act('quiz.skipQuestion', questionNumber);
        skipping = null;
    }
</script>

<div class="quiz">
    <div class="heading">
        <p class="progress">
            {fill(fr.modes.quiz.question, {
                number: view.questionNumber,
                count: view.questionCount,
            })}
        </p>
        {#if view.answersCloseAt !== null}
            <p class="countdown">
                <Countdown closeAt={view.answersCloseAt} {clock} label={fr.modes.quiz.timeLeft} />
            </p>
        {:else if view.phase === 'Locked'}
            <p class="time-up">{fr.modes.quiz.timeUp}</p>
        {/if}
    </div>
    <h3>{view.text}</h3>
    <ol class="choices" aria-label={fr.modes.quiz.choicesLabel}>
        {#each view.choices as choice (choice.letter)}
            <li class:correct={choice.correct} style:--choice-color={choiceColor(choice.letter)}>
                <ChoiceMarker letter={choice.letter} />
                <span class="text">{choice.text}</span>
                {#if choice.correct}
                    <CorrectMark />
                {/if}
                {#if view.phase !== 'Presentation'}
                    <span class="count"
                        >{countText(fr.modes.quiz.choiceAnswers, choice.answerCount)}</span
                    >
                {/if}
                {#if view.phase === 'Revealed' && chosenBy(choice.letter).length > 0}
                    <!-- The same distribution as on the TV screen, once revealed. -->
                    <span class="chosen-by">{chosenBy(choice.letter).join(' · ')}</span>
                {/if}
            </li>
        {/each}
    </ol>

    {#if view.phase !== 'Presentation'}
        <p class="answered" role="status">
            {fill(fr.modes.quiz.answered, {
                answered: answeredCount,
                participants: view.answers.length,
            })}{#if allAnswered}
                · <strong>{fr.modes.quiz.gm.allAnswered}</strong>{/if}
        </p>
        <ul class="players" aria-label={fr.modes.quiz.gm.answersLabel}>
            {#each view.answers as answer (answer.playerId)}
                <li>
                    <!-- Plain text interpolation: Svelte escapes it, so a nickname is never read as HTML. -->
                    <span class="nickname">{answer.nickname}</span>
                    {#if answer.choice !== null}
                        <ChoiceMarker letter={answer.choice} />
                    {:else}
                        <span class="none">
                            {view.phase === 'Answering'
                                ? fr.modes.quiz.gm.waitingAnswer
                                : fr.modes.quiz.noAnswer}
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
                onclick={() => act('quiz.openAnswers')}
            >
                {fr.modes.quiz.gm.openAnswers}
            </button>
        {:else if view.phase === 'Answering'}
            <button
                type="button"
                disabled={!interactive || sending}
                onclick={() => act('quiz.lockAnswers')}
            >
                {fr.modes.quiz.gm.lockAnswers}
            </button>
        {:else if view.phase === 'Locked'}
            <button
                type="button"
                disabled={!interactive || sending}
                onclick={() => act('quiz.revealAnswer')}
            >
                {fr.modes.quiz.gm.revealAnswer}
            </button>
        {/if}
        {#if view.phase === 'Revealed'}
            <button
                type="button"
                disabled={!interactive || sending}
                onclick={() => act('quiz.nextQuestion')}
            >
                {lastQuestion ? fr.modes.quiz.gm.endRound : fr.modes.quiz.gm.nextQuestion}
            </button>
        {:else}
            <button
                type="button"
                class="secondary"
                disabled={!interactive || sending}
                onclick={() => (skipping = view.questionNumber)}
            >
                {fr.modes.quiz.gm.skipQuestion}
            </button>
        {/if}
    </div>
</div>
{#if confirmingSkip}
    <ConfirmDialog
        title={fill(fr.modes.quiz.gm.skipConfirm.title, { number: view.questionNumber })}
        message={lastQuestion
            ? fr.modes.quiz.gm.skipConfirm.lastMessage
            : fr.modes.quiz.gm.skipConfirm.message}
        confirmLabel={fr.modes.quiz.gm.skipConfirm.confirm}
        cancelLabel={fr.modes.quiz.gm.skipConfirm.cancel}
        confirmDisabled={!interactive || sending}
        onconfirm={() => skip(view.questionNumber)}
        oncancel={() => (skipping = null)}
    />
{/if}

<style>
    .quiz {
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

    .progress {
        font-weight: 700;
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

    .choices,
    .players {
        display: flex;
        flex-direction: column;
        gap: var(--space-s);
        margin: 0;
        padding: 0;
        list-style: none;
    }

    .choices li {
        display: flex;
        flex-wrap: wrap;
        align-items: center;
        gap: var(--space-s) var(--space-m);
        padding: var(--space-s) var(--space-m);
        border-left: 0.5rem solid var(--choice-color);
        border-radius: var(--radius);
        background: var(--color-surface);
    }

    .choices li.correct {
        outline: 2px solid var(--color-text);
    }

    .text {
        flex: 1 1 auto;
        min-width: 0;
        overflow-wrap: anywhere;
    }

    .chosen-by {
        flex-basis: 100%;
        overflow-wrap: anywhere;
    }

    .count,
    .none,
    .answered {
        color: var(--color-text-muted);
    }

    .answered strong {
        color: var(--color-accent);
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
