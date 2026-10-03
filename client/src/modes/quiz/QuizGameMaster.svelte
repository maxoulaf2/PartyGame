<script lang="ts">
    import type { QuizGameMasterIntent, QuizGameMasterView } from '../../shared/contracts';
    import { fill } from '../../shared/i18n/fill';
    import { fr } from '../../shared/i18n/fr';
    import type { GameMasterViewProps } from '../../shared/modeViews';

    // Provisional: nothing to drive until the questions are played (US-E08-02).
    let { round }: GameMasterViewProps<QuizGameMasterView, QuizGameMasterIntent> = $props();
</script>

<p>{fill(fr.modes.quiz.gm.ready, { title: round.title })}</p>

<style>
    p {
        margin: 0;
        color: var(--color-text-muted);
    }
</style>
