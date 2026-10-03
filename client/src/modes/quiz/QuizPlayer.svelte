<script lang="ts">
    import Countdown from '../../shared/components/Countdown.svelte';
    import type {
        QuizChoiceLetter,
        QuizPlayerView,
        QuizSubmitAnswer,
    } from '../../shared/contracts';
    import { fill } from '../../shared/i18n/fill';
    import { fr } from '../../shared/i18n/fr';
    import type { PlayerViewProps } from '../../shared/modeViews';
    import ChoiceMarker from './ChoiceMarker.svelte';
    import { choiceColor } from './choiceTheme';

    // An answer pad: the question and the choices are read on the TV screen, so that players look
    // up from their phones. Each button has the letter, the shape and the color of its choice.
    let {
        view,
        round,
        clock,
        interactive,
        send,
    }: PlayerViewProps<QuizPlayerView, QuizSubmitAnswer> = $props();

    // The choice sent and not confirmed yet, shown at once. The snapshot always wins: once it
    // holds the answer, or leaves the answers, the pending choice no longer shows.
    let pending = $state<{ readonly question: number; readonly letter: QuizChoiceLetter } | null>(
        null,
    );
    const pendingLetter = $derived(
        pending !== null &&
            pending.question === view.questionNumber &&
            view.phase === 'Answering' &&
            view.answer === null
            ? pending.letter
            : null,
    );
    const chosen = $derived(view.answer ?? pendingLetter);
    const canAnswer = $derived(
        interactive && view.phase === 'Answering' && view.participating && chosen === null,
    );

    const status = $derived.by(() => {
        if (!view.participating) {
            return fr.modes.quiz.player.nextQuestion;
        }
        if (view.phase === 'Locked') {
            return fr.modes.quiz.timeUp;
        }
        if (view.answer !== null) {
            return fr.modes.quiz.player.recorded;
        }
        return pendingLetter !== null ? fr.modes.quiz.player.pending : null;
    });

    async function choose(letter: QuizChoiceLetter) {
        if (!canAnswer) {
            return;
        }
        const question = view.questionNumber;
        pending = { question, letter };
        await send({
            type: 'quiz.submitAnswer',
            roundId: round.roundId,
            questionNumber: question,
            choice: letter,
        });
        // Handled by the server by now: the snapshot that confirms the answer, or contradicts it,
        // arrived first. Not sent: the choice is free again once the connection is back.
        pending = null;
    }
</script>

<main>
    <header>
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
        {/if}
    </header>
    <ol class="choices" class:decided={chosen !== null} aria-label={fr.modes.quiz.choicesLabel}>
        {#each view.choices as letter (letter)}
            <li>
                <!-- Chosen on click, never on pointerdown: a finger sliding over the pad must not
                     answer by mistake. -->
                <button
                    type="button"
                    class:chosen={chosen === letter}
                    class:pending={pendingLetter === letter}
                    disabled={!canAnswer}
                    aria-pressed={chosen === letter}
                    aria-label={fill(fr.modes.quiz.player.choiceLabel, { letter })}
                    style:background={choiceColor(letter)}
                    onclick={() => choose(letter)}
                >
                    <ChoiceMarker {letter} color="currentColor" />
                </button>
            </li>
        {/each}
    </ol>
    <p class="status" role="status">{status ?? ''}</p>
</main>

<style>
    main {
        display: flex;
        flex-direction: column;
        gap: var(--space-m);
        min-height: 100vh;
        min-height: 100dvh;
        padding: var(--space-l) var(--space-m);
    }

    p {
        margin: 0;
        color: var(--color-text-muted);
        text-align: center;
    }

    header {
        display: flex;
        justify-content: center;
        align-items: center;
        gap: var(--space-l);
    }

    .progress {
        font-weight: 700;
    }

    .countdown {
        color: var(--color-accent);
        font-size: 1.75rem;
    }

    .choices {
        display: grid;
        flex: 1 1 auto;
        grid-template-columns: 1fr 1fr;
        grid-auto-rows: 1fr;
        gap: var(--space-m);
        margin: 0;
        padding: 0;
        list-style: none;
    }

    button {
        display: flex;
        align-items: center;
        justify-content: center;
        width: 100%;
        height: 100%;
        min-height: 6rem;
        border: none;
        border-radius: var(--radius);
        /* Dark on the light colors of the choices: the shape and the letter stay contrasted. */
        color: var(--color-bg);
        font: inherit;
        font-size: 2.5rem;
        cursor: pointer;
        touch-action: manipulation;
    }

    /* Waiting for the answers to open, or another choice taken: dimmed, still recognizable. */
    button:disabled {
        opacity: 0.6;
        cursor: default;
    }

    .decided button:not(.chosen) {
        opacity: 0.35;
    }

    /* The choice taken stands out by a ring, never by its color alone. */
    button.chosen {
        opacity: 1;
        outline: 0.375rem solid var(--color-text);
        outline-offset: 0.25rem;
    }

    /* Sent, not confirmed yet. */
    button.pending {
        outline-style: dashed;
    }

    .status {
        min-height: 1.5em;
        color: var(--color-text);
        font-size: 1.25rem;
        font-weight: 700;
    }
</style>
