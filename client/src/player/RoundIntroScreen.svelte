<script lang="ts">
    import Confetti from '../shared/components/Confetti.svelte';
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
    <Confetti />
    <p class="progress">{roundText(fr.game.round, round)}</p>
    <h1>{round.title}</h1>
    {#if mode}
        <p class="rule" aria-label={fr.game.ruleLabel}>{mode.rule}</p>
    {/if}
</main>

<style>
    main {
        position: relative;
        isolation: isolate;
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        gap: 28px;
        min-height: 100vh;
        min-height: 100dvh;
        padding: 40px 24px;
        overflow: hidden;
        text-align: center;
    }

    h1,
    p {
        margin: 0;
        overflow-wrap: anywhere;
    }

    .progress {
        padding: 8px 18px;
        border: var(--sticker-line) solid var(--color-ink);
        border-radius: 999px;
        background: var(--color-surface);
        box-shadow: 0 4px 0 var(--color-ink);
        color: var(--color-on-surface);
        font-size: 18px;
        font-weight: 800;
    }

    h1 {
        font-size: 44px;
        font-weight: 800;
        line-height: 1.05;
        letter-spacing: -0.02em;
        -webkit-text-stroke: 1.5px var(--color-ink);
        text-shadow: 0 4px 0 var(--color-ink);
    }

    .rule {
        padding: 20px 24px;
        border: var(--sticker-line) solid var(--color-ink);
        border-radius: 28px;
        background: var(--color-surface);
        box-shadow: 0 8px 0 var(--color-ink);
        color: var(--color-on-surface);
        font-size: 19px;
        font-weight: 700;
        line-height: 1.3;
        transform: rotate(-2deg);
    }
</style>
