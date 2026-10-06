<script lang="ts">
    import type { PlayerStanding } from '../shared/contracts';
    import { fr } from '../shared/i18n/fr';
    import { rankParts, standingText } from '../shared/i18n/rankText';

    interface Props {
        /** The rank of the player, computed by the server. */
        standing: PlayerStanding;
        /** A gold medal, for a player on the podium of the final ranking, else a card. */
        medal?: boolean;
    }

    let { standing, medal = false }: Props = $props();

    const rank = $derived(rankParts(fr.game.rank, standing.rank));
</script>

<!-- Read as a whole by screen readers: « 4e sur 8 ». -->
<p
    class:medal
    role="img"
    aria-label={standingText(fr.game.standing, fr.game.rank, standing, standing.rankedCount)}
>
    <span class="rank">{rank.number}<sup>{rank.ordinal}</sup></span>
    <span class="of"
        >{standingText(fr.game.standingOf, fr.game.rank, standing, standing.rankedCount)}</span
    >
</p>

<style>
    p {
        display: flex;
        flex-direction: column;
        align-items: center;
        gap: 4px;
        margin: 0;
        padding: 28px 40px;
        border: var(--sticker-line) solid var(--color-ink);
        border-radius: 28px;
        background: var(--color-surface);
        box-shadow: 0 8px 0 var(--color-ink);
        color: var(--color-on-surface);
    }

    .rank {
        font-size: 110px;
        font-weight: 800;
        line-height: 1;
        letter-spacing: -0.05em;
    }

    sup {
        font-size: 0.4em;
        letter-spacing: 0;
    }

    .of {
        font-size: 22px;
        font-weight: 700;
    }

    .medal {
        justify-content: center;
        gap: 0;
        width: 230px;
        height: 230px;
        padding: 0;
        border-radius: 50%;
        background: var(--color-accent);
        box-shadow:
            0 10px 0 var(--color-ink),
            inset 0 0 0 14px var(--color-orange),
            inset 0 0 0 17px var(--color-ink);
    }

    .medal .rank {
        font-size: 104px;
        line-height: 0.9;
    }
</style>
