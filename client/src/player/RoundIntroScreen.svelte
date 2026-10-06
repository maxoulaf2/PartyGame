<script lang="ts">
    import type { RoundInfo } from '../shared/contracts';
    import { roundText } from '../shared/i18n/fill';
    import { fr } from '../shared/i18n/fr';
    import { findModeTexts } from '../modes/registry';

    interface Props {
        /** The round announced, which the game master has yet to start. */
        round: RoundInfo;
    }

    let { round }: Props = $props();

    // Nothing to tap: the round starts when the game master says so.
    const mode = $derived(findModeTexts(round.mode));
</script>

<main>
    <p class="progress">{roundText(fr.game.round, round)}</p>
    <h1>{round.title}</h1>
    {#if mode}
        <p class="rule" aria-label={fr.game.ruleLabel}>{mode.rule}</p>
    {/if}
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
        overflow-wrap: anywhere;
    }

    .progress {
        color: var(--color-text-muted);
        font-weight: 700;
    }

    h1 {
        color: var(--color-accent);
        font-size: var(--font-size-title);
        line-height: 1.15;
    }

    .rule {
        font-size: 1.125rem;
    }
</style>
