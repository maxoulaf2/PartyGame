<script lang="ts">
    import type { QuizDisplayView } from '../../shared/contracts';
    import { roundText } from '../../shared/i18n/fill';
    import { fr } from '../../shared/i18n/fr';
    import type { DisplayViewProps } from '../../shared/modeViews';

    // Provisional: the round title only, until the questions are played (US-E08-02).
    let { round }: DisplayViewProps<QuizDisplayView> = $props();
</script>

<main>
    <p class="progress">{roundText(fr.game.round, round)}</p>
    <h1>{round.title}</h1>
    <p class="hint">{fr.modes.quiz.display.ready}</p>
</main>

<style>
    /* TVs may crop their edges (overscan): nothing essential within 5% of any border. */
    main {
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        gap: 4vh;
        height: 100vh;
        padding: 5vh 5vw;
        overflow: hidden;
        text-align: center;
    }

    h1,
    p {
        margin: 0;
    }

    h1 {
        color: var(--color-accent);
        font-size: var(--font-size-title);
        line-height: 1.1;
        overflow-wrap: anywhere;
    }

    .progress {
        font-weight: 700;
    }

    .hint {
        color: var(--color-text-muted);
    }
</style>
