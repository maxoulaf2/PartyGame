<script lang="ts">
    import type { RankedPlayer } from '../shared/contracts';
    import { countText } from '../shared/i18n/countText';
    import { fr } from '../shared/i18n/fr';
    import { rankText } from '../shared/i18n/rankText';

    interface Props {
        /** Ranked by the server, ties in alphabetical order: shown as received, never sorted here. */
        ranking: readonly RankedPlayer[];
    }

    let { ranking }: Props = $props();
</script>

<ol aria-label={fr.game.rankingLabel}>
    {#each ranking as player (player.id)}
        <li>
            <span class="rank">{rankText(fr.game.rank, player.rank)}</span>
            <!-- Plain text interpolation: Svelte escapes it, so a nickname is never read as HTML. -->
            <span class="nickname">{player.nickname}</span>
            <span class="score">{countText(fr.game.points, player.score)}</span>
        </li>
    {/each}
</ol>

<style>
    ol {
        display: flex;
        flex-direction: column;
        gap: var(--space-s);
        margin: 0;
        padding: 0;
        list-style: none;
    }

    li {
        display: flex;
        align-items: baseline;
        gap: var(--space-m);
        padding: var(--space-s) var(--space-m);
        border-radius: var(--radius);
        background: var(--color-surface);
    }

    .rank {
        flex: 0 0 3em;
        color: var(--color-accent);
        font-weight: 700;
    }

    .nickname {
        flex: 1;
        min-width: 0;
        font-weight: 700;
        /* A long nickname wraps rather than being cut. */
        overflow-wrap: anywhere;
    }

    .score {
        white-space: nowrap;
    }
</style>
