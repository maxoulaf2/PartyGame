<script lang="ts">
    import type { PlayerStanding } from '../shared/contracts';
    import { countText } from '../shared/i18n/countText';
    import { fr } from '../shared/i18n/fr';
    import { standingText } from '../shared/i18n/rankText';
    import { isOnPodium } from '../shared/podium';

    interface Props {
        /**
         * The final rank of the player, computed by the server, or null for a phone that joined
         * once the game was finished.
         */
        standing: PlayerStanding | null;
        score: number;
    }

    let { standing, score }: Props = $props();
</script>

<main>
    <h1>{fr.game.finished}</h1>
    {#if standing}
        <p class="standing">
            {standingText(fr.game.standing, fr.game.rank, standing, standing.rankedCount)}
        </p>
        <p class="score">{countText(fr.game.points, score)}</p>
        <p class="message">{isOnPodium(standing.rank) ? fr.player.podium : fr.player.finished}</p>
    {:else}
        <p class="message">{fr.player.joinedAfterEnd}</p>
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

    .message {
        font-size: 1.25rem;
        font-weight: 700;
    }
</style>
