<script lang="ts">
    import Confetti from '../shared/components/Confetti.svelte';
    import type { PlayerStanding, RoundInfo } from '../shared/contracts';
    import { countText } from '../shared/i18n/countText';
    import { roundText } from '../shared/i18n/fill';
    import { fr } from '../shared/i18n/fr';
    import StandingBadge from './StandingBadge.svelte';

    interface Props {
        /** The round that just finished. */
        round: RoundInfo;
        /** The rank of the player, computed by the server. */
        standing: PlayerStanding;
        score: number;
    }

    let { round, standing, score }: Props = $props();
</script>

<main>
    <Confetti
        colors={[
            'var(--color-accent)',
            'var(--color-surface)',
            'var(--color-pink)',
            'var(--color-green)',
        ]}
    />
    <h1>{roundText(fr.game.roundEnded, round)}</h1>
    <StandingBadge {standing} />
    <p class="score">{countText(fr.game.points, score)}</p>
    <p class="status">{fr.player.betweenRounds}</p>
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
        background: var(--color-blue);
        color: var(--color-ink);
        text-align: center;
    }

    h1,
    p {
        margin: 0;
    }

    h1 {
        color: var(--color-surface);
        font-size: 32px;
        font-weight: 800;
        line-height: 1.05;
        letter-spacing: -0.02em;
        -webkit-text-stroke: 1.5px var(--color-ink);
    }

    .score {
        padding: 10px 20px;
        border-radius: 999px;
        background: var(--color-ink);
        color: var(--color-surface);
        font-size: 22px;
        font-weight: 800;
    }

    .status {
        font-size: 19px;
        font-weight: 700;
        line-height: 1.3;
        text-wrap: pretty;
    }
</style>
