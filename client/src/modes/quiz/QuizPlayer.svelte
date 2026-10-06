<script lang="ts">
    import Confetti from '../../shared/components/Confetti.svelte';
    import Countdown from '../../shared/components/Countdown.svelte';
    import type {
        QuizChoiceLetter,
        QuizPlayerView,
        QuizSubmitAnswer,
    } from '../../shared/contracts';
    import { countText } from '../../shared/i18n/countText';
    import { fill } from '../../shared/i18n/fill';
    import { fr } from '../../shared/i18n/fr';
    import { formatNumber } from '../../shared/i18n/numberText';
    import type { PlayerViewProps } from '../../shared/modeViews';
    import ChoiceMarker from './ChoiceMarker.svelte';
    import { choiceColor } from './choiceTheme';

    // An answer pad: the question and the choices are read on the TV screen, so that players look
    // up from their phones. Each button has the letter, the shape and the color of its choice.
    let {
        view,
        round,
        score,
        clock,
        interactive,
        send,
        pending,
    }: PlayerViewProps<QuizPlayerView, QuizSubmitAnswer> = $props();

    // The answers open with the first choice the TV screen shows: each button unlocks as its
    // choice shows, so that the players may answer while the game master reads out the others.
    const answersOpen = $derived(
        view.phase === 'Answering' || (view.phase === 'Presentation' && view.shownChoiceCount > 0),
    );

    // The choice sent and not acknowledged yet, shown at once, even after a reload. The snapshot
    // always wins: once it holds the answer, or leaves the answers, the pending choice no longer
    // shows.
    const pendingLetter = $derived(
        answersOpen && view.answer === null
            ? (pending.find((intent) => intent.questionNumber === view.questionNumber)?.choice ??
                  null)
            : null,
    );
    const chosen = $derived(view.answer ?? pendingLetter);
    const canAnswer = $derived(interactive && answersOpen && view.participating && chosen === null);

    const status = $derived.by(() => {
        if (view.phase === 'Revealed') {
            return null;
        }
        if (!view.participating) {
            return fr.modes.quiz.player.nextQuestion;
        }
        if (view.answer !== null) {
            return fr.modes.quiz.player.recorded;
        }
        // Without an answer, only the end of the countdown locks them: the others lock once everybody answered.
        if (view.phase === 'Locked') {
            return fr.modes.quiz.timeUp;
        }
        if (pendingLetter !== null) {
            return fr.modes.quiz.player.pending;
        }
        return answersOpen ? fr.modes.quiz.player.choose : null;
    });
    const recorded = $derived(view.phase !== 'Revealed' && view.answer !== null);

    // Once revealed, the whole phone takes the color of the verdict, read from across the table.
    const revealed = $derived(view.correctChoice !== null);

    /** Whether the TV screen shows the choice at `index`, in the order of the letters. */
    function shown(index: number): boolean {
        return index < view.shownChoiceCount;
    }

    function choose(letter: QuizChoiceLetter, index: number) {
        if (!canAnswer || !shown(index)) {
            return;
        }
        send({
            type: 'quiz.submitAnswer',
            roundId: round.roundId,
            questionNumber: view.questionNumber,
            choice: letter,
        });
    }
</script>

<main class:revealed data-verdict={revealed ? view.verdict : null}>
    {#if revealed && view.verdict === 'Correct'}
        <Confetti
            colors={[
                'var(--color-accent)',
                'var(--color-surface)',
                'var(--color-blue)',
                'var(--color-pink)',
            ]}
        />
    {/if}
    <header>
        <p class="progress">
            {fill(fr.modes.quiz.question, {
                number: view.questionNumber,
                count: view.questionCount,
            })}
        </p>
        {#if view.answersCloseAt !== null}
            <p class="countdown">
                <Countdown
                    closeAt={view.answersCloseAt}
                    {clock}
                    label={fr.modes.quiz.timeLeft}
                    ring
                />
            </p>
        {/if}
    </header>
    {#if view.correctChoice !== null}
        <!-- The verdict, told by an icon and a text, then the correct choice by its letter, its
             shape and its color: its text is read on the TV screen. -->
        {#if view.verdict === 'Correct'}
            <div class="check" aria-hidden="true">
                <svg viewBox="0 0 24 24"><path d="M4 12.5l5 5L20 6.5" /></svg>
            </div>
            <p class="verdict">{fr.modes.quiz.player.verdicts.Correct}</p>
        {:else if view.verdict !== null}
            <p class="verdict">
                <svg viewBox="0 0 24 24" aria-hidden="true">
                    {#if view.verdict === 'Wrong'}
                        <path d="M6 6l12 12M18 6L6 18" />
                    {:else}
                        <path d="M6 12h12" />
                    {/if}
                </svg>
                {fr.modes.quiz.player.verdicts[view.verdict]}
            </p>
        {/if}
        {#if view.points !== null}
            <!-- Both computed by the server: the phone adds nothing up. -->
            <div class="points">
                <p class="earned">
                    {fill(fr.modes.quiz.pointsEarned, { points: formatNumber(view.points) })}
                </p>
                <p class="score">{countText(fr.modes.quiz.player.score, score)}</p>
            </div>
        {/if}
        {#if view.verdict !== 'Correct'}
            <p class="correct-label">{fr.modes.quiz.player.correctChoice}</p>
            <div
                class="correct-choice"
                role="img"
                aria-label={fill(fr.modes.quiz.player.choiceLabel, {
                    letter: view.correctChoice,
                })}
                style:background={choiceColor(view.correctChoice)}
            >
                <ChoiceMarker letter={view.correctChoice} color="currentColor" stacked />
            </div>
        {/if}
    {:else}
        <ol class="choices" class:decided={chosen !== null} aria-label={fr.modes.quiz.choicesLabel}>
            {#each view.choices as letter, index (letter)}
                <li>
                    <!-- Chosen on click, never on pointerdown: a finger sliding over the pad must not
                         answer by mistake. -->
                    <button
                        type="button"
                        class:chosen={chosen === letter}
                        class:pending={pendingLetter === letter}
                        disabled={!canAnswer || !shown(index)}
                        aria-pressed={chosen === letter}
                        aria-label={fill(fr.modes.quiz.player.choiceLabel, { letter })}
                        style:background={choiceColor(letter)}
                        onclick={() => choose(letter, index)}
                    >
                        <ChoiceMarker {letter} color="currentColor" stacked />
                    </button>
                </li>
            {/each}
        </ol>
    {/if}
    <p class="status" class:recorded role="status">
        {#if recorded}
            <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M4 12.5l5 5L20 6.5" /></svg>
        {/if}
        {status ?? ''}
    </p>
</main>

<style>
    main {
        position: relative;
        isolation: isolate;
        display: flex;
        flex-direction: column;
        gap: 20px;
        min-height: 100vh;
        min-height: 100dvh;
        padding: 56px 18px 32px;
        overflow: hidden;
        color: var(--color-ink);
    }

    p {
        margin: 0;
        text-align: center;
    }

    header {
        display: flex;
        justify-content: space-between;
        align-items: center;
    }

    .progress {
        padding: 8px 16px;
        border: var(--sticker-line) solid var(--color-ink);
        border-radius: 999px;
        background: var(--color-surface);
        box-shadow: 0 4px 0 var(--color-ink);
        font-size: 17px;
        font-weight: 800;
    }

    .countdown {
        font-size: 26px;
    }

    .choices {
        display: grid;
        flex: 1 1 auto;
        grid-template-columns: 1fr 1fr;
        grid-auto-rows: 1fr;
        gap: 16px;
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
        border: var(--sticker-line) solid var(--color-ink);
        border-radius: 26px;
        box-shadow: 0 8px 0 var(--color-ink);
        /* Ink on the light colors of the choices: the shape and the letter stay contrasted. */
        color: var(--color-ink);
        font: inherit;
        font-size: 46px;
        cursor: pointer;
        touch-action: manipulation;
    }

    button:active:enabled {
        transform: translateY(5px);
        box-shadow: 0 3px 0 var(--color-ink);
    }

    /* Not on the TV screen yet, or another choice taken: dimmed, still recognizable. */
    button:disabled {
        opacity: 0.6;
        cursor: default;
    }

    .decided button:not(.chosen) {
        opacity: 0.35;
        box-shadow: 0 3px 0 var(--color-ink);
    }

    /* The choice taken stands out by a ring and a tilt, never by its color alone. */
    button.chosen {
        opacity: 1;
        outline: 5px solid var(--color-surface);
        outline-offset: 5px;
        transform: rotate(-2deg) scale(1.03);
    }

    /* Sent, not confirmed yet. */
    button.pending {
        outline-style: dashed;
    }

    .status {
        display: flex;
        align-items: center;
        justify-content: center;
        gap: 8px;
        align-self: center;
        min-height: 30px;
        color: var(--color-text);
        font-size: 18px;
        font-weight: 700;
    }

    .status.recorded {
        padding: 8px 18px 8px 12px;
        border: var(--sticker-line) solid var(--color-ink);
        border-radius: 999px;
        background: var(--color-green);
        box-shadow: 0 4px 0 var(--color-ink);
        color: var(--color-ink);
        font-weight: 800;
    }

    svg {
        flex: none;
        fill: none;
        stroke: currentColor;
        stroke-linecap: round;
        stroke-linejoin: round;
    }

    .status svg {
        width: 22px;
        height: 22px;
        stroke-width: 3.5;
    }

    /* The reveal: the verdict floods the phone, readable from across the table. */
    .revealed {
        align-items: center;
        justify-content: center;
        gap: 22px;
        padding: 40px 24px;
        text-align: center;
    }

    .revealed[data-verdict='Correct'] {
        gap: 28px;
        background: var(--color-green);
    }

    .revealed[data-verdict='Wrong'] {
        background: var(--color-pink);
    }

    /* On the violet ground, without a verdict to flood it. */
    .revealed:not([data-verdict='Correct'], [data-verdict='Wrong']) {
        color: var(--color-text);
    }

    .revealed header {
        position: absolute;
        top: 56px;
        left: 50%;
        transform: translateX(-50%);
    }

    .revealed .progress {
        padding: 6px 14px;
        box-shadow: none;
        color: var(--color-on-surface);
        font-size: 15px;
        white-space: nowrap;
    }

    .revealed .status {
        display: none;
    }

    .check {
        display: grid;
        place-items: center;
        width: 120px;
        height: 120px;
        border: var(--sticker-line) solid var(--color-ink);
        border-radius: 50%;
        background: var(--color-surface);
        box-shadow: 0 7px 0 var(--color-ink);
    }

    .check svg {
        width: 72px;
        height: 72px;
        stroke: var(--color-ink);
        stroke-width: 3;
    }

    .verdict {
        display: flex;
        align-items: center;
        gap: 12px;
        font-size: 56px;
        font-weight: 800;
        line-height: 1;
        letter-spacing: -0.02em;
    }

    .verdict svg {
        width: 52px;
        height: 52px;
        stroke-width: 3.5;
    }

    [data-verdict='Correct'] .verdict {
        font-size: 44px;
    }

    .points {
        display: flex;
        flex-direction: column;
        align-items: center;
        gap: 6px;
    }

    .earned {
        color: var(--color-surface);
        font-size: 40px;
        font-weight: 800;
        -webkit-text-stroke: 2px var(--color-ink);
    }

    .score {
        font-size: 18px;
        font-weight: 700;
    }

    [data-verdict='Correct'] .points {
        gap: 28px;
    }

    [data-verdict='Correct'] .earned {
        font-size: 128px;
        line-height: 0.9;
        letter-spacing: -0.05em;
        -webkit-text-stroke-width: 4px;
        text-shadow: 0 8px 0 var(--color-ink);
    }

    [data-verdict='Correct'] .score {
        padding: 10px 20px;
        border-radius: 999px;
        background: var(--color-ink);
        color: var(--color-surface);
        font-size: 20px;
    }

    .correct-label {
        margin-top: 12px;
        font-size: 20px;
        font-weight: 700;
    }

    .correct-choice {
        display: flex;
        align-items: center;
        justify-content: center;
        width: 220px;
        height: 220px;
        border: var(--sticker-line) solid var(--color-ink);
        border-radius: 32px;
        box-shadow: 0 9px 0 var(--color-ink);
        /* Ink on the light colors of the choices, as on the pad. */
        color: var(--color-ink);
        font-size: 64px;
        transform: rotate(-3deg);
    }
</style>
