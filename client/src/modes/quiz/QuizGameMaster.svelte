<script lang="ts">
    import type { QuizGameMasterIntent, QuizGameMasterView } from '../../shared/contracts';
    import { fill } from '../../shared/i18n/fill';
    import { fr } from '../../shared/i18n/fr';
    import type { GameMasterViewProps } from '../../shared/modeViews';
    import ChoiceMarker from './ChoiceMarker.svelte';
    import { choiceColor } from './choiceTheme';

    let { view }: GameMasterViewProps<QuizGameMasterView, QuizGameMasterIntent> = $props();
</script>

<div class="quiz">
    <p class="progress">
        {fill(fr.modes.quiz.question, { number: view.questionNumber, count: view.questionCount })}
    </p>
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
            </li>
        {/each}
    </ol>
    <!-- Opening the answers comes with US-E08-03, skipping a question with US-E08-05. -->
    <div class="actions">
        <button type="button" disabled>{fr.modes.quiz.gm.openAnswers}</button>
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

    .progress {
        font-weight: 700;
    }

    h3 {
        font-size: 1.25rem;
        overflow-wrap: anywhere;
    }

    .choices {
        display: flex;
        flex-direction: column;
        gap: var(--space-s);
        margin: 0;
        padding: 0;
        list-style: none;
    }

    li {
        display: flex;
        align-items: center;
        gap: var(--space-m);
        padding: var(--space-s) var(--space-m);
        border-left: 0.5rem solid var(--choice-color);
        border-radius: var(--radius);
        background: var(--color-surface);
    }

    li.correct {
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
