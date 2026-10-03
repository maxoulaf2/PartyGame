<script lang="ts">
    import type { QuizPlayerIntent, QuizPlayerView } from '../../shared/contracts';
    import { fill } from '../../shared/i18n/fill';
    import { fr } from '../../shared/i18n/fr';
    import type { PlayerViewProps } from '../../shared/modeViews';
    import ChoiceMarker from './ChoiceMarker.svelte';
    import { choiceColor } from './choiceTheme';

    // An answer pad: the question and the choices are read on the TV screen, so that players look
    // up from their phones. Each button has the letter, the shape and the color of its choice.
    let { view }: PlayerViewProps<QuizPlayerView, QuizPlayerIntent> = $props();
</script>

<main>
    <p class="progress">
        {fill(fr.modes.quiz.question, { number: view.questionNumber, count: view.questionCount })}
    </p>
    <ol class="choices" aria-label={fr.modes.quiz.choicesLabel}>
        {#each view.choices as letter (letter)}
            <li>
                <!-- Nothing to choose while the question is presented: the answers open next. -->
                <button
                    type="button"
                    disabled
                    aria-label={fill(fr.modes.quiz.player.choiceLabel, { letter })}
                    style:background={choiceColor(letter)}
                >
                    <ChoiceMarker {letter} color="currentColor" />
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

    p {
        margin: 0;
        color: var(--color-text-muted);
        text-align: center;
    }

    .progress {
        font-weight: 700;
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
        touch-action: manipulation;
    }

    /* Waiting for the answers to open, not out of order: dimmed, still recognizable. */
    button:disabled {
        opacity: 0.6;
    }
</style>
