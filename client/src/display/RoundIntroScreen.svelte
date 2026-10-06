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

    // A mode this build does not know still gets its number and title, without a rule.
    const mode = $derived(findModeTexts(round.mode));
</script>

<main>
    <Confetti tv />
    <p class="progress">{roundText(fr.game.round, round)}</p>
    <h1>{round.title}</h1>
    {#if mode}
        <p class="mode">{mode.name}</p>
        <p class="rule" aria-label={fr.game.ruleLabel}>{mode.rule}</p>
    {/if}
    {#if round.description}
        <!-- Plain text interpolation: Svelte escapes it, so the pack never injects HTML. -->
        <p class="description">{round.description}</p>
    {/if}
</main>

<style>
    /* TVs may crop their edges (overscan): nothing essential within 5% of any border. Sized so that
       a description of 300 characters fits under the rule without scrolling. */
    main {
        position: relative;
        isolation: isolate;
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        gap: 3vh;
        height: 100vh;
        padding: 5vh 5vw;
        overflow: hidden;
        text-align: center;
    }

    p,
    h1 {
        margin: 0;
        max-width: 80vw;
        overflow-wrap: anywhere;
    }

    .progress {
        padding: 0.15em 0.8em;
        border-radius: 999px;
        background: var(--color-ink);
        color: var(--color-surface);
        font-size: 4vh;
        font-weight: 700;
    }

    h1 {
        font-size: 9vh;
        font-weight: 800;
        line-height: 1.1;
        letter-spacing: -0.02em;
        -webkit-text-stroke: calc(2 * var(--u)) var(--color-ink);
        text-shadow: 0 calc(5 * var(--u)) 0 var(--color-ink);
    }

    .mode {
        padding: 0.2em 0.9em;
        border: var(--sticker-line) solid var(--color-ink);
        border-radius: 999px;
        background: var(--color-accent);
        box-shadow: 0 calc(4 * var(--u)) 0 var(--color-ink);
        color: var(--color-ink);
        font-size: 4.5vh;
        font-weight: 800;
        transform: rotate(-2deg);
    }

    .rule {
        padding: 0.5em 1em;
        border: var(--sticker-line) solid var(--color-ink);
        border-radius: calc(26 * var(--u));
        background: var(--color-surface);
        box-shadow: 0 calc(8 * var(--u)) 0 var(--color-ink);
        color: var(--color-on-surface);
        font-size: 4vh;
        font-weight: 700;
        line-height: 1.35;
    }

    .description {
        color: var(--color-text-muted);
        font-size: 3.6vh;
        font-weight: 600;
        line-height: 1.35;
    }
</style>
