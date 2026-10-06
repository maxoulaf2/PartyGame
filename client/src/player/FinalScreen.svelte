<script lang="ts">
    import Confetti from '../shared/components/Confetti.svelte';
    import type { ServerClock } from '../shared/connection/clockSync.svelte';
    import type { PlayerStanding } from '../shared/contracts';
    import { countText } from '../shared/i18n/countText';
    import { fr } from '../shared/i18n/fr';
    import { isOnPodium, isRevealed, untilNextReveal } from '../shared/podium';
    import StandingBadge from './StandingBadge.svelte';

    interface Props {
        /**
         * The final rank of the player, computed by the server, or null for a phone that joined
         * once the game was finished.
         */
        standing: PlayerStanding | null;
        score: number;
        /** When the game finished, on the clock of the server: the TV screen reveals the ranks from then on. */
        finishedAt: number | null;
        clock: Pick<ServerClock, 'serverNow'>;
    }

    let { standing, score, finishedAt, clock }: Props = $props();

    // The rank shows once the TV screen reveals it, not to spoil the podium.
    let ticks = $state(0);
    const now = $derived.by(() => {
        void ticks;
        return clock.serverNow();
    });
    $effect(() => {
        const wait = untilNextReveal(finishedAt, now);
        if (wait === null) {
            return;
        }
        const timer = setTimeout(() => ticks++, wait);
        return () => clearTimeout(timer);
    });
    const revealed = $derived(standing !== null && isRevealed(standing.rank, finishedAt, now));
</script>

<main>
    <Confetti />
    <h1>{fr.game.finished}</h1>
    {#if standing && !revealed}
        <p class="message" role="status">{fr.player.revealing}</p>
    {:else if standing}
        <StandingBadge {standing} medal={isOnPodium(standing.rank)} />
        <p class="score">{countText(fr.game.points, score)}</p>
        <p class="message">{isOnPodium(standing.rank) ? fr.player.podium : fr.player.finished}</p>
    {:else}
        <p class="message">{fr.player.joinedAfterEnd}</p>
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
        gap: 26px;
        min-height: 100vh;
        min-height: 100dvh;
        padding: 40px 24px;
        overflow: hidden;
        text-align: center;
    }

    h1,
    p {
        margin: 0;
    }

    h1 {
        padding: 8px 18px;
        border: var(--sticker-line) solid var(--color-ink);
        border-radius: 999px;
        background: var(--color-surface);
        box-shadow: 0 4px 0 var(--color-ink);
        color: var(--color-on-surface);
        font-size: 18px;
        font-weight: 800;
    }

    .score {
        padding: 10px 20px;
        border-radius: 999px;
        background: var(--color-ink);
        color: var(--color-surface);
        font-size: 22px;
        font-weight: 800;
    }

    .message {
        font-size: 26px;
        font-weight: 800;
        line-height: 1.15;
        text-wrap: pretty;
    }
</style>
