<script lang="ts">
    import type { QuizPlayerIntent, QuizPlayerView } from '../../shared/contracts';
    import { roundText } from '../../shared/i18n/fill';
    import { fr } from '../../shared/i18n/fr';
    import type { PlayerViewProps } from '../../shared/modeViews';

    // Provisional: the round title only, until the questions are played (US-E08-02).
    let { round }: PlayerViewProps<QuizPlayerView, QuizPlayerIntent> = $props();
</script>

<main>
    <p class="progress">{roundText(fr.game.round, round)}</p>
    <h1>{round.title}</h1>
    <p class="hint">{fr.modes.quiz.player.ready}</p>
</main>

<style>
    main {
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        gap: var(--space-m);
        min-height: 100vh;
        min-height: 100dvh;
        padding: var(--space-l);
        text-align: center;
    }

    h1,
    p {
        margin: 0;
    }

    h1 {
        color: var(--color-accent);
        font-size: var(--font-size-title);
        overflow-wrap: anywhere;
    }

    .progress,
    .hint {
        color: var(--color-text-muted);
    }
</style>
