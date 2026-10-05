<script lang="ts">
    import Countdown from '../../shared/components/Countdown.svelte';
    import { sessionCodeStorage } from '../../shared/connection/codeStorage';
    import type { OpenQuestionPlayerView, OpenQuestionSubmitAnswer } from '../../shared/contracts';
    import { countText } from '../../shared/i18n/countText';
    import { fill } from '../../shared/i18n/fill';
    import { fr } from '../../shared/i18n/fr';
    import { formatNumber } from '../../shared/i18n/numberText';
    import type { PlayerViewProps } from '../../shared/modeViews';

    // A field to type the answer in: the question is read on the TV screen, so that players look
    // up from their phones.
    let {
        view,
        round,
        score,
        clock,
        interactive,
        send,
        pending,
    }: PlayerViewProps<OpenQuestionPlayerView, OpenQuestionSubmitAnswer> = $props();

    // The text typed and not sent yet survives a reload of the tab, as when the phone wakes up: it
    // is kept for the question it was typed for, as « roundId/question\ntext ».
    const draftStorage = sessionCodeStorage('partygame.openquestion.draft');
    const draftKey = $derived(`${round.roundId}/${view.questionNumber}`);
    let typed = $state(restoreDraft());
    const draft = $derived(typed.key === draftKey ? typed.text : '');

    function restoreDraft(): { key: string; text: string } {
        const saved = draftStorage.load() ?? '';
        const separator = saved.indexOf('\n');
        return separator < 0
            ? { key: '', text: '' }
            : { key: saved.slice(0, separator), text: saved.slice(separator + 1) };
    }

    function edit(text: string) {
        typed = { key: draftKey, text };
        draftStorage.save(`${draftKey}\n${text}`);
    }

    const answersOpen = $derived(view.phase === 'Answering');

    // The answer sent and not acknowledged yet, shown at once, even after a reload. The snapshot
    // always wins: once it holds the answer, or leaves the answers, the pending one no longer shows.
    const pendingAnswer = $derived(
        answersOpen && view.answer === null
            ? (pending.find((intent) => intent.questionNumber === view.questionNumber)?.answer ??
                  null)
            : null,
    );
    const sent = $derived(view.answer ?? pendingAnswer);
    const canAnswer = $derived(interactive && answersOpen && view.participating && sent === null);

    const status = $derived.by(() => {
        if (view.phase === 'Revealed') {
            return null;
        }
        if (!view.participating) {
            return fr.modes.openquestion.player.nextQuestion;
        }
        if (view.answer !== null) {
            return fr.modes.openquestion.player.recorded;
        }
        if (pendingAnswer !== null) {
            return fr.modes.openquestion.player.pending;
        }
        // Without an answer, only the end of the countdown locks them: the others lock once everybody answered.
        if (view.phase === 'Locked' || view.phase === 'Judged') {
            return fr.modes.openquestion.timeUp;
        }
        return view.phase === 'Presentation' ? fr.modes.openquestion.player.waitQuestion : null;
    });

    function submit(event: SubmitEvent) {
        event.preventDefault();
        // Sent as typed: the server alone normalizes it, and never truncates it.
        if (!canAnswer || draft.trim() === '') {
            return;
        }
        send({
            type: 'openquestion.submitAnswer',
            roundId: round.roundId,
            questionNumber: view.questionNumber,
            answer: draft,
        });
    }
</script>

<main>
    <header>
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
        {/if}
    </header>
    <!-- At the top of the page, so that the keyboard never hides it. Enter sends, as the button. -->
    <form onsubmit={submit}>
        <label for="open-answer">{fr.modes.openquestion.player.answerLabel}</label>
        <input
            id="open-answer"
            type="text"
            inputmode={view.numeric ? 'numeric' : 'text'}
            enterkeyhint="send"
            autocomplete="off"
            autocorrect="off"
            autocapitalize="off"
            spellcheck="false"
            maxlength={view.maxLength}
            class:pending={pendingAnswer !== null}
            value={sent ?? draft}
            disabled={!canAnswer}
            oninput={(event) => edit(event.currentTarget.value)}
        />
        <button type="submit" disabled={!canAnswer || draft.trim() === ''}>
            {fr.modes.openquestion.player.send}
        </button>
    </form>
    <p class="status" role="status">{status ?? ''}</p>
    {#if view.expectedAnswer !== null}
        <section class="reveal">
            {#if view.verdict !== null}
                <!-- Told by an icon and a text, never by the color alone. -->
                <p class="verdict {view.verdict}">
                    <svg viewBox="0 0 24 24" width="1.25em" height="1.25em" aria-hidden="true">
                        {#if view.verdict === 'Correct'}
                            <path d="M4 12.5l5 5L20 6.5" />
                        {:else if view.verdict === 'Wrong'}
                            <path d="M6 6l12 12M18 6L6 18" />
                        {:else}
                            <path d="M6 12h12" />
                        {/if}
                    </svg>
                    {fr.modes.openquestion.player.verdicts[view.verdict]}
                </p>
            {/if}
            <p class="expected">
                {fill(fr.modes.openquestion.player.expectedAnswer, {
                    answer: view.expectedAnswer,
                })}
            </p>
            {#if view.points !== null}
                <!-- Both computed by the server: the phone adds nothing up. -->
                <p class="earned">
                    {fill(fr.modes.openquestion.pointsEarned, {
                        points: formatNumber(view.points),
                    })}
                </p>
                <p class="score">{countText(fr.modes.openquestion.player.score, score)}</p>
            {/if}
        </section>
    {/if}
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

    form {
        display: flex;
        flex-direction: column;
        gap: var(--space-s);
    }

    label {
        color: var(--color-text-muted);
        font-weight: 700;
    }

    input {
        min-height: var(--touch-target-min);
        padding: var(--space-s) var(--space-m);
        border: 2px solid var(--color-text-muted);
        border-radius: var(--radius);
        background: var(--color-surface);
        color: var(--color-text);
        font: inherit;
        /* 16 px at least: Safari zooms on a smaller field. */
        font-size: 1.5rem;
        touch-action: manipulation;
    }

    input:focus {
        border-color: var(--color-accent);
        outline: none;
    }

    input:disabled {
        opacity: 0.8;
    }

    /* Sent, not confirmed yet. */
    input.pending {
        border-style: dashed;
    }

    button {
        min-height: var(--touch-target-min);
        padding: 0 var(--space-l);
        border: none;
        border-radius: var(--radius);
        background: var(--color-accent);
        color: var(--color-bg);
        font: inherit;
        font-size: 1.25rem;
        font-weight: 700;
        cursor: pointer;
        touch-action: manipulation;
    }

    button:disabled {
        opacity: 0.4;
        cursor: default;
    }

    .reveal {
        display: flex;
        flex-direction: column;
        align-items: center;
        gap: var(--space-s);
    }

    .verdict {
        display: inline-flex;
        align-items: center;
        gap: 0.3em;
        color: var(--color-text);
        font-size: 2.25rem;
        font-weight: 800;
    }

    .verdict svg {
        fill: none;
        stroke: currentColor;
        stroke-width: 3;
        stroke-linecap: round;
        stroke-linejoin: round;
    }

    .verdict.Correct {
        color: var(--color-accent);
    }

    .expected {
        color: var(--color-text);
        font-size: 1.25rem;
        overflow-wrap: anywhere;
    }

    .earned {
        color: var(--color-text);
        font-size: 2rem;
        font-weight: 800;
    }

    .score {
        font-size: 1.25rem;
    }

    .status {
        min-height: 1.5em;
        color: var(--color-text);
        font-size: 1.25rem;
        font-weight: 700;
    }
</style>
