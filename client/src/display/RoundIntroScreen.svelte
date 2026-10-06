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

    // A mode this build does not know still gets its number and title, without a rule.
    const mode = $derived(findModeTexts(round.mode));
</script>

<main>
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
        color: var(--color-text-muted);
        font-size: 4vh;
        font-weight: 700;
    }

    h1 {
        color: var(--color-accent);
        font-size: 9vh;
        line-height: 1.1;
    }

    .mode {
        padding: 0.3em 1em;
        border-radius: var(--radius);
        background: var(--color-surface);
        font-size: 4.5vh;
        font-weight: 800;
    }

    .rule {
        font-size: 4vh;
        line-height: 1.35;
    }

    .description {
        color: var(--color-text-muted);
        font-size: 3.6vh;
        line-height: 1.35;
    }
</style>
