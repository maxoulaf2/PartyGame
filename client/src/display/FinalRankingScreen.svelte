<script lang="ts">
    import ConnectionIcon from '../shared/components/ConnectionIcon.svelte';
    import type { RankedPlayer } from '../shared/contracts';
    import { countText } from '../shared/i18n/countText';
    import { fill } from '../shared/i18n/fill';
    import { fr } from '../shared/i18n/fr';
    import { rankText } from '../shared/i18n/rankText';
    import { podiumPosition, splitFinalRanking } from '../shared/podium';
    import { playerListLayout } from './playerListLayout';

    interface Props {
        /** Ranked by the server, ties in alphabetical order: shown as received, never sorted here. */
        ranking: readonly RankedPlayer[];
    }

    let { ranking }: Props = $props();

    const final = $derived(splitFinalRanking(ranking));
    // Sized for every player, so that the podium and the rest read alike.
    const layout = $derived(playerListLayout(ranking.length));
</script>

<main style:--nickname-size={layout.fontSize}>
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
                <li class="step" style:order={podiumPosition(step.rank)}>
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
        display: flex;
        flex-direction: column;
        gap: 2.5vh;
        height: 100vh;
        padding: 5vh 5vw;
        overflow: hidden;
    }

    header {
        display: flex;
        flex-direction: column;
        align-items: center;
        gap: 0.5vh;
        text-align: center;
    }

    h1,
    p {
        margin: 0;
    }

    h1 {
        color: var(--color-accent);
        font-size: 6.5vh;
        line-height: 1.1;
    }

    .progress {
        color: var(--color-text-muted);
        font-weight: 700;
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
        gap: 1.5vw;
        font-size: var(--nickname-size);
        font-weight: 700;
        line-height: 1.25;
    }

    /* Its names side by side when the screen is wide enough, else on several rows: the step with
       the most ex aequo gives up the most width, but never below its longest nickname. */
    .step {
        display: flex;
        flex: 0 1 auto;
        flex-direction: column;
        gap: 0.8vh;
    }

    .step ul {
        display: flex;
        flex-wrap: wrap;
        align-content: flex-end;
        justify-content: center;
        gap: 0.6vh 0.8vw;
    }

    .step li {
        display: flex;
        align-items: center;
        gap: 0.4em;
        padding: 0.15em 0.6em;
        border-radius: var(--radius);
        background: var(--color-surface);
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
        min-width: 20vw;
        border-radius: var(--radius) var(--radius) 0 0;
        background: var(--color-accent);
        color: var(--color-bg);
        line-height: 1.1;
        text-align: center;
    }

    /* The higher the rank, the higher the step. */
    .block[data-rank='1'] {
        height: 14vh;
    }

    .block[data-rank='2'] {
        height: 11vh;
    }

    .block[data-rank='3'] {
        height: 8vh;
    }

    .block .rank {
        font-size: 4.2vh;
    }

    .block .score {
        font-size: 2.8vh;
    }

    /* Filled column by column, so that the ranks read top to bottom. */
    .rest {
        display: grid;
        grid-template-rows: repeat(var(--rows), auto);
        grid-template-columns: repeat(var(--columns), minmax(0, 1fr));
        grid-auto-flow: column;
        align-content: start;
        gap: 0.6vh 2vw;
        /* A score stays close to its nickname, even in a single column. */
        width: 100%;
        max-width: calc(var(--columns) * 45vw);
        margin: 0 auto;
        font-size: var(--nickname-size);
        font-weight: 700;
        line-height: 1.25;
    }

    .rest li {
        display: flex;
        align-items: center;
        gap: 0.5em;
        min-width: 0;
        padding: 0.15em 0.5em;
        border-radius: var(--radius);
        background: var(--color-surface);
    }

    .rest .rank {
        flex: 0 0 2.6em;
        color: var(--color-accent);
    }

    .rest .nickname {
        flex: 1;
        min-width: 0;
        /* A nickname too wide for its column wraps rather than being cut. */
        overflow-wrap: anywhere;
    }

    .rest .score {
        flex: 0 0 auto;
        white-space: nowrap;
    }

    .disconnected .nickname {
        opacity: 0.45;
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
