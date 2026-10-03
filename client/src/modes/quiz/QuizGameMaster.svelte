<script lang="ts">
    import Countdown from '../../shared/components/Countdown.svelte';
    import type {
        QuizGameMasterView,
        QuizLockAnswers,
        QuizOpenAnswers,
    } from '../../shared/contracts';
    import { countText } from '../../shared/i18n/countText';
    import { fill } from '../../shared/i18n/fill';
    import { fr } from '../../shared/i18n/fr';
    import type { GameMasterViewProps } from '../../shared/modeViews';
    import ChoiceMarker from './ChoiceMarker.svelte';
    import { choiceColor } from './choiceTheme';

    let {
        view,
        round,
        clock,
        interactive,
        send,
    }: GameMasterViewProps<QuizGameMasterView, QuizOpenAnswers | QuizLockAnswers> = $props();

    let sending = $state(false);

    const answeredCount = $derived(view.answers.filter((answer) => answer.choice !== null).length);
    const allAnswered = $derived(view.answers.length > 0 && answeredCount === view.answers.length);

    async function act(type: 'quiz.openAnswers' | 'quiz.lockAnswers') {
        if (!interactive || sending) {
            return;
        }
        sending = true;
        // Each intent names its question: sent again, or by a second console, it changes nothing.
        // A lost connection is for the connection indicator to show.
        await send({ type, roundId: round.roundId, questionNumber: view.questionNumber });
        sending = false;
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
                    <!-- Told by an icon and a label, never by the color alone. -->
                    <span class="answer">
                        <svg viewBox="0 0 24 24" width="1.25em" height="1.25em" aria-hidden="true">
                            <path
                                d="M4 12.5l5 5L20 6.5"
                                fill="none"
                                stroke="currentColor"
                                stroke-width="3"
                                stroke-linecap="round"
                                stroke-linejoin="round"
                            />
                        </svg>
                        {fr.modes.quiz.gm.correct}
                    </span>
                {/if}
                {#if view.phase !== 'Presentation'}
                    <span class="count"
                        >{countText(fr.modes.quiz.gm.choiceAnswers, choice.answerCount)}</span
                    >
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
                                : fr.modes.quiz.gm.noAnswer}
                        </span>
                    {/if}
                </li>
            {/each}
        </ul>
    {/if}

    <!-- Revealing comes with US-E08-04, skipping a question with US-E08-05. -->
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
        {/if}
        <button type="button" class="secondary" disabled>{fr.modes.quiz.gm.skipQuestion}</button>
    </div>
</div>

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

    .answer {
        display: inline-flex;
        flex: none;
        align-items: center;
        gap: 0.25em;
        font-weight: 700;
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
