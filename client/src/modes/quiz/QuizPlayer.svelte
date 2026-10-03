<script lang="ts">
    import type { QuizPlayerIntent, QuizPlayerView } from '../../shared/contracts';
    import { fill } from '../../shared/i18n/fill';
    import { fr } from '../../shared/i18n/fr';
    import type { PlayerViewProps } from '../../shared/modeViews';
    import ChoiceMarker from './ChoiceMarker.svelte';
    import { choiceColor } from './choiceTheme';

    let { view }: PlayerViewProps<QuizPlayerView, QuizPlayerIntent> = $props();
</script>

<main>
    <p class="progress">
        {fill(fr.modes.quiz.question, { number: view.questionNumber, count: view.questionCount })}
    </p>
    <h1>{view.text}</h1>
    <ol class="choices" aria-label={fr.modes.quiz.choicesLabel}>
        {#each view.choices as choice (choice.letter)}
            <li>
                <!-- Nothing to choose while the question is presented: the answers open next. -->
                <button type="button" disabled style:--choice-color={choiceColor(choice.letter)}>
                    <ChoiceMarker letter={choice.letter} />
                    <span class="text">{choice.text}</span>
                </button>
            </li>
        {/each}
    </ol>
    <p class="hint" role="status">{fr.modes.quiz.player.presentation}</p>
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

    h1,
    p {
        margin: 0;
    }

    .progress,
    .hint {
        color: var(--color-text-muted);
        text-align: center;
    }

    .progress {
        font-weight: 700;
    }

    h1 {
        font-size: 1.375rem;
        line-height: 1.3;
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

    button {
        display: flex;
        align-items: center;
        gap: var(--space-m);
        width: 100%;
        min-height: var(--touch-target-min);
        padding: var(--space-s) var(--space-m);
        border: 2px solid var(--choice-color);
        border-left-width: 0.5rem;
        border-radius: var(--radius);
        background: var(--color-surface);
        color: var(--color-text);
        font: inherit;
        font-size: 1.125rem;
        text-align: left;
        touch-action: manipulation;
    }

    /* Waiting for the answers to open, not out of order: dimmed, still readable. */
    button:disabled {
        opacity: 0.75;
    }

    .text {
        min-width: 0;
        overflow-wrap: anywhere;
    }
</style>
