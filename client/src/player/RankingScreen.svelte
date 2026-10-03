<script lang="ts">
    import type { PlayerStanding, RoundInfo } from '../shared/contracts';
    import { countText } from '../shared/i18n/countText';
    import { roundText } from '../shared/i18n/fill';
    import { fr } from '../shared/i18n/fr';
    import { standingText } from '../shared/i18n/rankText';

    interface Props {
        /** The round that just finished. */
        round: RoundInfo;
        /** The rank of the player, computed by the server. */
        standing: PlayerStanding;
        /** Every registered player is ranked. */
        playerCount: number;
        score: number;
    }

    let { round, standing, playerCount, score }: Props = $props();
</script>

<main>
    <h1>{roundText(fr.game.roundEnded, round)}</h1>
    <p class="standing">{standingText(fr.game.standing, fr.game.rank, standing, playerCount)}</p>
    <p class="score">{countText(fr.game.points, score)}</p>
    <p class="status">{fr.player.betweenRounds}</p>
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
    }

    .standing {
        font-size: 2.5rem;
        font-weight: 700;
        line-height: 1.15;
    }

    .score {
        font-size: 1.5rem;
        font-weight: 700;
    }

    .status {
        color: var(--color-text-muted);
    }
</style>
