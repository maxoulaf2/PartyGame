<script lang="ts">
    import ConfirmDialog from '../../shared/components/ConfirmDialog.svelte';
    import Countdown from '../../shared/components/Countdown.svelte';
    import type {
        QuizChoiceLetter,
        QuizGameMasterView,
        QuizNextQuestion,
        QuizRevealAnswer,
        QuizShowChoice,
        QuizShowQuestion,
        QuizSkipQuestion,
    } from '../../shared/contracts';
    import { countText } from '../../shared/i18n/countText';
    import { fill } from '../../shared/i18n/fill';
    import { fr } from '../../shared/i18n/fr';
    import { formatNumber } from '../../shared/i18n/numberText';
    import type { GameMasterViewProps } from '../../shared/modeViews';
    import ChoiceMarker from './ChoiceMarker.svelte';
    import { choiceColor } from './choiceTheme';
    import CorrectMark from './CorrectMark.svelte';

    let { view, round, clock, interactive, send }: GameMasterViewProps<QuizGameMasterView, Intent> =
        $props();

    type Intent =
        QuizShowQuestion | QuizShowChoice | QuizRevealAnswer | QuizNextQuestion | QuizSkipQuestion;

    let sending = $state(false);
    // The question the game master asked to skip, until they confirm or cancel. The dialog goes away
    // on its own once the round moves on, for instance by a second console.
    let skipping = $state<number | null>(null);
    const confirmingSkip = $derived(skipping === view.questionNumber && view.phase !== 'Revealed');

    const lastQuestion = $derived(view.questionNumber === view.questionCount);

    // The game master reads out the question, then each choice, and shows it on the TV screen
    // right after: the choice to show next, once the question is. The last one starts the
    // countdown.
    const nextChoice = $derived(
        view.phase === 'Presentation' && view.questionShown
            ? (view.choices.find((choice) => !choice.shown) ?? null)
            : null,
    );
    // The players answer from the first choice shown, while the game master reads out the others.
    const answersOpened = $derived(view.choices.some((choice) => choice.shown));

    const answeredCount = $derived(view.answers.filter((answer) => answer.choice !== null).length);
    const allAnswered = $derived(view.answers.length > 0 && answeredCount === view.answers.length);

    /** The nicknames of the players who chose `letter`, in order of arrival. */
    function chosenBy(letter: QuizChoiceLetter): string[] {
        return view.answers
            .filter((answer) => answer.choice === letter)
            .map((answer) => answer.nickname);
    }

    async function act(intent: Intent) {
        if (!interactive || sending) {
            return;
        }
        sending = true;
        // Each intent names its question, and the step it moves on from: sent again, or by a
        // second console, it changes nothing. A lost connection is for the connection indicator
        // to show.
        await send(intent);
        sending = false;
    }

    function step(
        type: Exclude<Intent['type'], 'quiz.showChoice'>,
        questionNumber = view.questionNumber,
    ) {
        return act({ type, roundId: round.roundId, questionNumber });
    }

    function showChoice(choice: QuizChoiceLetter) {
        return act({
            type: 'quiz.showChoice',
            roundId: round.roundId,
            questionNumber: view.questionNumber,
            choice,
        });
    }

    async function skip(questionNumber: number) {
        await step('quiz.skipQuestion', questionNumber);
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
        {:else if view.phase === 'Locked' && !allAnswered}
            <!-- Locked early once everybody answered: the line of answers already says so. -->
            <p class="time-up">{fr.modes.quiz.timeUp}</p>
        {/if}
    </div>
    <!-- The console shows the whole question from the start, for the game master to read it out;
         what the TV screen does not show yet is marked. -->
    <div class="question" class:hidden={!view.questionShown}>
        <h3>{view.text}</h3>
        {#if !view.questionShown}
            <span class="hidden-label">{fr.modes.quiz.gm.hiddenOnDisplay}</span>
        {/if}
    </div>
    <ol class="choices" aria-label={fr.modes.quiz.choicesLabel}>
        {#each view.choices as choice (choice.letter)}
            <li
                class:correct={choice.correct}
                class:hidden={!choice.shown}
                style:--choice-color={choiceColor(choice.letter)}
            >
                <ChoiceMarker letter={choice.letter} />
                <span class="text">{choice.text}</span>
                {#if choice.correct}
                    <CorrectMark />
                {/if}
                {#if !choice.shown}
                    <span class="hidden-label">{fr.modes.quiz.gm.hiddenOnDisplay}</span>
                {/if}
                {#if choice.shown}
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

    {#if answersOpened}
        <p class="answered" role="status">
            {fill(fr.modes.quiz.answered, {
                answered: answeredCount,
                participants: view.answers.length,
            })}{#if allAnswered}
                · <strong>{fr.modes.quiz.allAnswered}</strong>{/if}
        </p>
        <ul class="players" aria-label={fr.modes.quiz.gm.answersLabel}>
            {#each view.answers as answer (answer.playerId)}
                <li>
                    <!-- Plain text interpolation: Svelte escapes it, so a nickname is never read as HTML. -->
                    <span class="nickname">{answer.nickname}</span>
                    <span class="answer">
                        {#if answer.choice !== null}
                            <ChoiceMarker letter={answer.choice} />
                        {:else}
                            <span class="none">
                                {view.phase === 'Answering' || view.phase === 'Presentation'
                                    ? fr.modes.quiz.gm.waitingAnswer
                                    : fr.modes.quiz.noAnswer}
                            </span>
                        {/if}
                        {#if answer.points !== null}
                            <span class="points">
                                {fill(fr.modes.quiz.pointsEarned, {
                                    points: formatNumber(answer.points),
                                })}
                            </span>
                        {/if}
                    </span>
                </li>
            {/each}
        </ul>
    {/if}

    <div class="actions">
        {#if view.phase === 'Presentation'}
            {#if !view.questionShown}
                <button
                    type="button"
                    disabled={!interactive || sending}
                    onclick={() => step('quiz.showQuestion')}
                >
                    {fr.modes.quiz.gm.showQuestion}
                </button>
            {:else if nextChoice !== null}
                {@const letter = nextChoice.letter}
                <button
                    type="button"
                    disabled={!interactive || sending}
                    onclick={() => showChoice(letter)}
                >
                    {fill(fr.modes.quiz.gm.showChoice, { letter })}
                </button>
            {/if}
        {:else if view.phase === 'Locked'}
            <button
                type="button"
                disabled={!interactive || sending}
                onclick={() => step('quiz.revealAnswer')}
            >
                {fr.modes.quiz.gm.revealAnswer}
            </button>
        {/if}
        {#if view.phase === 'Revealed'}
            <button
                type="button"
                disabled={!interactive || sending}
                onclick={() => step('quiz.nextQuestion')}
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

    .question {
        display: flex;
        flex-direction: column;
        gap: var(--space-s);
    }

    /* Not on the TV screen yet: dashed rather than faded alone, so that the text stays easy to
       read out. */
    .question.hidden,
    .choices li.hidden {
        outline: 2px dashed var(--color-text-muted);
        outline-offset: 2px;
        border-radius: var(--radius);
    }

    .hidden-label {
        color: var(--color-text-muted);
        font-size: 0.875rem;
        font-style: italic;
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

    .answer {
        display: inline-flex;
        align-items: center;
        gap: var(--space-m);
    }

    .points {
        min-width: 4.5em;
        font-weight: 700;
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
