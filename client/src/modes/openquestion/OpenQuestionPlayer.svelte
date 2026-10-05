<script lang="ts">
    import Countdown from '../../shared/components/Countdown.svelte';
    import { sessionCodeStorage } from '../../shared/connection/codeStorage';
    import type { OpenQuestionPlayerView, OpenQuestionSubmitAnswer } from '../../shared/contracts';
    import { fill } from '../../shared/i18n/fill';
    import { fr } from '../../shared/i18n/fr';
    import type { PlayerViewProps } from '../../shared/modeViews';

    // A field to type the answer in: the question is read on the TV screen, so that players look
    // up from their phones.
    let {
        view,
        round,
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
        if (view.phase === 'Locked') {
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

    .status {
        min-height: 1.5em;
        color: var(--color-text);
        font-size: 1.25rem;
        font-weight: 700;
    }
</style>
