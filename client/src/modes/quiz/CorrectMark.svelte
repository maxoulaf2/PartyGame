<script lang="ts">
    import { fr } from '../../shared/i18n/fr';
</script>

<!-- Told by an icon and a label, never by the color alone. -->
<span class="correct">
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
    {fr.modes.quiz.correct}
</span>

<style>
    .correct {
        display: inline-flex;
        flex: none;
        align-items: center;
        gap: 0.25em;
        font-weight: 700;
    }
</style>
