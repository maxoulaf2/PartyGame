<script lang="ts">
    import ConfirmDialog from '../../shared/components/ConfirmDialog.svelte';
    import Countdown from '../../shared/components/Countdown.svelte';
    import type {
        OpenQuestionGameMasterGroup,
        OpenQuestionGameMasterView,
        OpenQuestionJudge,
        OpenQuestionNextQuestion,
        OpenQuestionRevealAnswer,
        OpenQuestionShowQuestion,
        OpenQuestionSkipQuestion,
    } from '../../shared/contracts';
    import { fill } from '../../shared/i18n/fill';
    import { fr } from '../../shared/i18n/fr';
    import { formatNumber } from '../../shared/i18n/numberText';
    import type { GameMasterViewProps } from '../../shared/modeViews';

    type Intent =
        | OpenQuestionShowQuestion
        | OpenQuestionSkipQuestion
        | OpenQuestionJudge
        | OpenQuestionRevealAnswer
        | OpenQuestionNextQuestion;

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
    const confirmingSkip = $derived(skipping === view.questionNumber && view.phase !== 'Revealed');

    const lastQuestion = $derived(view.questionNumber === view.questionCount);
    const answeredCount = $derived(view.answers.filter((answer) => answer.answer !== null).length);
    const allAnswered = $derived(view.answers.length > 0 && answeredCount === view.answers.length);

    const locked = $derived(
        view.phase === 'Locked' || view.phase === 'Judged' || view.phase === 'Revealed',
    );
    // Each author with their points once revealed, both computed by the server.
    const authors = $derived(
        new Map(
            view.answers.map((answer) => [
                answer.playerId,
                answer.points === null
                    ? answer.nickname
                    : `${answer.nickname} (${fill(fr.modes.openquestion.pointsEarned, { points: formatNumber(answer.points) })})`,
            ]),
        ),
    );
    const withoutAnswer = $derived(
        view.answers.filter((answer) => answer.answer === null).map((answer) => answer.nickname),
    );

    // The boxes the game master changed, until they validate: the accepted answers start checked.
    // Kept per question, so that the next one starts from its own suggestions.
    let changed = $state<Record<string, boolean>>({});
    const keyOf = (index: number) => `${round.roundId}/${view.questionNumber}/${index}`;
    const isChecked = (group: OpenQuestionGameMasterGroup, index: number) =>
        changed[keyOf(index)] ?? group.category === 'Accepted';

    async function judge() {
        if (!interactive || sending) {
            return;
        }
        sending = true;
        // One intent for the whole batch: sent again, or by a second console, it is obsolete.
        await send({
            type: 'openquestion.judge',
            roundId: round.roundId,
            questionNumber: view.questionNumber,
            acceptedPlayers: view.groups.flatMap((group, index) =>
                isChecked(group, index) ? group.playerIds : [],
            ),
        });
        sending = false;
    }

    async function step(
        type: Exclude<Intent['type'], 'openquestion.judge'>,
        questionNumber = view.questionNumber,
    ) {
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
        {:else if locked && !allAnswered}
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
        {#if !locked}
            <ul class="players" aria-label={fr.modes.openquestion.gm.answersLabel}>
                {#each view.answers as answer (answer.playerId)}
                    <li>
                        <!-- Plain text interpolation: Svelte escapes nicknames and answers, never read as HTML. -->
                        <span class="nickname">{answer.nickname}</span>
                        {#if answer.answer !== null}
                            <span class="answer">{answer.answer}</span>
                        {:else}
                            <span class="none">{fr.modes.openquestion.gm.waitingAnswer}</span>
                        {/if}
                    </li>
                {/each}
            </ul>
        {:else}
            <!-- Identical answers once normalized make one line: accepted, then to check, then rejected. -->
            <ul class="players" aria-label={fr.modes.openquestion.gm.groupsLabel}>
                {#each view.groups as group, index (index)}
                    <li class:accepted={group.accepted === true}>
                        <label>
                            {#if view.phase === 'Locked'}
                                <input
                                    type="checkbox"
                                    checked={isChecked(group, index)}
                                    disabled={!interactive || sending}
                                    onchange={(event) =>
                                        (changed[keyOf(index)] = event.currentTarget.checked)}
                                />
                            {/if}
                            <span class="group">
                                <span class="answer">{group.text}</span>
                                <span class="authors">
                                    {group.playerIds.map((id) => authors.get(id) ?? '').join(', ')}
                                </span>
                            </span>
                            <span class="category">
                                {group.accepted === null
                                    ? fr.modes.openquestion.gm.categories[group.category]
                                    : group.accepted
                                      ? fr.modes.openquestion.gm.right
                                      : fr.modes.openquestion.gm.wrong}
                            </span>
                        </label>
                    </li>
                {/each}
            </ul>
            {#if withoutAnswer.length > 0}
                <p class="none">
                    {fill(fr.modes.openquestion.gm.withoutAnswer, {
                        nicknames: withoutAnswer.join(', '),
                    })}
                </p>
            {/if}
        {/if}
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
        {:else if view.phase === 'Locked'}
            <button type="button" disabled={!interactive || sending} onclick={judge}>
                {fr.modes.openquestion.gm.validate}
            </button>
        {:else if view.phase === 'Judged'}
            <button
                type="button"
                disabled={!interactive || sending}
                onclick={() => step('openquestion.revealAnswer')}
            >
                {fr.modes.openquestion.gm.revealAnswer}
            </button>
        {:else if view.phase === 'Revealed'}
            <button
                type="button"
                disabled={!interactive || sending}
                onclick={() => step('openquestion.nextQuestion')}
            >
                {lastQuestion
                    ? fr.modes.openquestion.gm.endRound
                    : fr.modes.openquestion.gm.nextQuestion}
            </button>
        {/if}
        {#if view.phase !== 'Revealed'}
            <button
                type="button"
                class="secondary"
                disabled={!interactive || sending}
                onclick={() => (skipping = view.questionNumber)}
            >
                {fr.modes.openquestion.gm.skipQuestion}
            </button>
        {/if}
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

    .players label {
        display: flex;
        flex: 1 1 auto;
        align-items: center;
        gap: var(--space-m);
        min-width: 0;
        min-height: var(--touch-target-min);
        cursor: pointer;
        touch-action: manipulation;
    }

    input[type='checkbox'] {
        flex: none;
        width: 1.5rem;
        height: 1.5rem;
        margin: 0;
        accent-color: var(--color-accent);
    }

    .group {
        display: flex;
        flex: 1 1 auto;
        flex-direction: column;
        min-width: 0;
    }

    .group .answer {
        font-weight: 700;
        text-align: left;
    }

    .authors {
        color: var(--color-text-muted);
        font-size: 0.875rem;
        overflow-wrap: anywhere;
    }

    .category {
        flex: none;
        color: var(--color-text-muted);
        font-weight: 700;
    }

    li.accepted .category {
        color: var(--color-accent);
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
