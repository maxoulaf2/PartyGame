<script lang="ts">
    import Confetti from '../shared/components/Confetti.svelte';
    import ConnectionIcon from '../shared/components/ConnectionIcon.svelte';
    import type { ServerClock } from '../shared/connection/clockSync.svelte';
    import type { RankedPlayer } from '../shared/contracts';
    import { countText } from '../shared/i18n/countText';
    import { fill } from '../shared/i18n/fill';
    import { fr } from '../shared/i18n/fr';
    import { rankText } from '../shared/i18n/rankText';
    import {
        isRevealed,
        podiumPosition,
        splitFinalRanking,
        untilNextReveal,
    } from '../shared/podium';
    import { playerListLayout } from './playerListLayout';

    interface Props {
        /** Ranked by the server, ties in alphabetical order: shown as received, never sorted here. */
        ranking: readonly RankedPlayer[];
        /** When the game finished, on the clock of the server: the podium is revealed from then on. */
        finishedAt: number | null;
        clock: Pick<ServerClock, 'serverNow'>;
    }

    let { ranking, finishedAt, clock }: Props = $props();

    // Read again at each step of the reveal, which the phones follow as well.
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

    const final = $derived(splitFinalRanking(ranking));
    // Sized for every player, so that the podium and the rest read alike.
    const layout = $derived(playerListLayout(ranking.length));
</script>

<!-- Beyond 12 players, the stickers slim down so that 20 long nicknames still fit. -->
<main style:--nickname-size={layout.fontSize} class:dense={ranking.length > 12}>
    <Confetti tv />
    <header>
        <p class="progress">{fr.game.finished}</p>
        <h1>{fr.game.finalRanking}</h1>
    </header>
    {#if final.steps.length > 0}
        <!-- In the order of the ranking for screen readers, laid out as a podium. -->
        <ol class="podium" aria-label={fr.game.podiumLabel}>
            {#each final.steps as step (step.rank)}
                {@const rank = rankText(fr.game.rank, step.rank)}
                <!-- Ex aequo share their step, which widens with them. -->
                <!-- Hidden until revealed, its place kept so that the podium never moves. -->
                <li
                    class="step"
                    class:hidden={!isRevealed(step.rank, finishedAt, now)}
                    style:order={podiumPosition(step.rank)}
                >
                    <ul aria-label={fill(fr.game.podiumStepLabel, { rank })}>
                        {#each step.players as player (player.id)}
                            <li class:disconnected={!player.isConnected}>
                                <!-- Plain text interpolation: Svelte escapes it, so a nickname is never read as HTML. -->
                                <span class="nickname">{player.nickname}</span>
                                {#if !player.isConnected}
                                    <!-- Dimmed and marked with an icon: never told apart by colour alone. -->
                                    <ConnectionIcon connected={false} />
                                    <span class="visually-hidden">({fr.display.disconnected})</span>
                                {/if}
                            </li>
                        {/each}
                    </ul>
                    <!-- Told apart by its written rank and its height, never by colour alone. Ex aequo
                         have the same score: the step shows it once. -->
                    <div class="block" data-rank={step.rank}>
                        <span class="rank">{rank}</span>
                        <span class="score">
                            {countText(fr.game.points, step.players[0]?.score ?? 0)}
                        </span>
                    </div>
                </li>
            {/each}
        </ol>
    {/if}
    {#if final.rest.length > 0}
        <ol
            class="rest"
            aria-label={fr.game.restLabel}
            style:--columns={layout.columns}
            style:--rows={Math.ceil(final.rest.length / layout.columns)}
        >
            {#each final.rest as player (player.id)}
                <li class:disconnected={!player.isConnected}>
                    <span class="rank">{rankText(fr.game.rank, player.rank)}</span>
                    <span class="nickname">{player.nickname}</span>
                    {#if !player.isConnected}
                        <ConnectionIcon connected={false} />
                        <span class="visually-hidden">({fr.display.disconnected})</span>
                    {/if}
                    <span class="score">{countText(fr.game.points, player.score)}</span>
                </li>
            {/each}
        </ol>
    {/if}
</main>

<style>
    /* TVs may crop their edges (overscan): nothing essential within 5% of any border. */
    main {
        position: relative;
        isolation: isolate;
        display: flex;
        flex-direction: column;
        gap: calc(14 * var(--u));
        height: 100vh;
        padding: 5vh 5vw;
        overflow: hidden;
        color: var(--color-ink);
    }

    header {
        display: flex;
        flex-direction: column;
        align-items: center;
        gap: calc(6 * var(--u));
        text-align: center;
    }

    h1,
    p {
        margin: 0;
    }

    .progress {
        padding: calc(4 * var(--u)) calc(14 * var(--u));
        border-radius: 999px;
        background: var(--color-ink);
        color: var(--color-surface);
        font-size: calc(16 * var(--u));
        font-weight: 700;
    }

    h1 {
        color: var(--color-surface);
        font-size: calc(42 * var(--u));
        font-weight: 800;
        line-height: 1;
        letter-spacing: -0.02em;
        -webkit-text-stroke: calc(1.5 * var(--u)) var(--color-ink);
        text-shadow: 0 calc(4 * var(--u)) 0 var(--color-ink);
    }

    ol,
    ul {
        margin: 0;
        padding: 0;
        list-style: none;
    }

    .podium {
        display: flex;
        flex: 0 0 auto;
        align-items: flex-end;
        justify-content: center;
        gap: calc(16 * var(--u));
        font-size: var(--nickname-size);
        font-weight: 800;
        line-height: 1.25;
    }

    /* Its names side by side when the screen is wide enough, else on several rows: the step with
       the most ex aequo gives up the most width, but never below its longest nickname. */
    .step {
        display: flex;
        flex: 0 1 auto;
        flex-direction: column;
        gap: calc(8 * var(--u));
    }

    .step.hidden {
        visibility: hidden;
        opacity: 0;
        transform: translateY(4vh);
    }

    @media (prefers-reduced-motion: no-preference) {
        .step {
            transition:
                opacity 0.8s ease-out,
                transform 0.8s ease-out,
                visibility 0.8s;
        }
    }

    .step ul {
        display: flex;
        flex-wrap: wrap;
        align-content: flex-end;
        justify-content: center;
        gap: calc(6 * var(--u));
    }

    .step li {
        display: flex;
        align-items: center;
        gap: 0.4em;
        padding: 0.1em 0.5em;
        border: var(--sticker-line) solid var(--color-ink);
        border-radius: calc(12 * var(--u));
        background: var(--color-surface);
        box-shadow: 0 calc(4 * var(--u)) 0 var(--color-ink);
    }

    .step .nickname {
        /* A nickname stays whole: its step widens to hold it. */
        white-space: nowrap;
    }

    .block {
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        min-width: calc(200 * var(--u));
        border: var(--sticker-line) solid var(--color-ink);
        border-bottom: none;
        border-radius: calc(18 * var(--u)) calc(18 * var(--u)) 0 0;
        background: var(--color-accent);
        line-height: 1.05;
        text-align: center;
    }

    /* The higher the rank, the higher the step. */
    .block[data-rank='1'] {
        height: calc(124 * var(--u));
    }

    .block[data-rank='2'] {
        height: calc(92 * var(--u));
        background: var(--color-blue);
    }

    .block[data-rank='3'] {
        height: calc(66 * var(--u));
        background: var(--color-pink);
    }

    .block .rank {
        font-size: calc(40 * var(--u));
        letter-spacing: -0.03em;
    }

    .block .score {
        font-size: calc(17 * var(--u));
        font-weight: 700;
    }

    /* Filled column by column, so that the ranks read top to bottom. */
    .rest {
        display: grid;
        grid-template-rows: repeat(var(--rows), auto);
        grid-template-columns: repeat(var(--columns), minmax(0, 1fr));
        grid-auto-flow: column;
        align-content: start;
        gap: calc(8 * var(--u)) calc(14 * var(--u));
        /* A score stays close to its nickname, even in a single column. */
        width: 100%;
        max-width: calc(var(--columns) * 45vw);
        margin: 0 auto;
        font-size: var(--nickname-size);
        font-weight: 800;
        line-height: 1.25;
    }

    .rest li {
        display: flex;
        align-items: center;
        gap: 0.5em;
        min-width: 0;
        padding: 0.1em 0.6em;
        border: calc(2.5 * var(--u)) solid var(--color-ink);
        border-radius: calc(12 * var(--u));
        background: var(--color-surface);
    }

    .rest .rank {
        flex: 0 0 2.6em;
        color: var(--color-bg);
    }

    .rest .nickname {
        flex: 1;
        min-width: 0;
        /* A nickname too wide for its column wraps rather than being cut. */
        overflow-wrap: anywhere;
    }

    .rest .score {
        flex: 0 0 auto;
        font-weight: 700;
        white-space: nowrap;
    }

    .disconnected .nickname {
        opacity: 0.45;
    }

    .dense {
        --sticker-line: calc(2 * var(--u));
        gap: 1.5vh;
    }

    .dense .block[data-rank='1'] {
        height: 14vh;
    }

    .dense .block[data-rank='2'] {
        height: 11vh;
    }

    .dense .block[data-rank='3'] {
        height: 8vh;
    }

    .dense .rest {
        gap: 0.6vh 2vw;
    }

    .dense .rest li {
        padding: 0 0.5em;
        border-width: calc(2 * var(--u));
    }

    .visually-hidden {
        position: absolute;
        width: 1px;
        height: 1px;
        overflow: hidden;
        clip-path: inset(50%);
        white-space: nowrap;
    }
</style>
