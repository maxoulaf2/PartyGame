<script lang="ts">
    import { onMount } from 'svelte';
    import ConnectionIcon from '../shared/components/ConnectionIcon.svelte';
    import QrCode from '../shared/components/QrCode.svelte';
    import type { DisplaySnapshot, RoundInfo } from '../shared/contracts';
    import { composeJoinUrl } from '../shared/connection/joinUrl';
    import { countText } from '../shared/i18n/countText';
    import { fill, roundText } from '../shared/i18n/fill';
    import { fr } from '../shared/i18n/fr';
    import { rankText } from '../shared/i18n/rankText';
    import { playerListLayout } from './playerListLayout';
    import { placesGained, previousIndexes } from './rankMoves';

    interface Props {
        snapshot: DisplaySnapshot;
        /** The round that just finished. */
        round: RoundInfo;
    }

    let { snapshot, round }: Props = $props();

    // Ranked by the server, ties in alphabetical order: shown as received, never sorted here.
    const ranking = $derived(snapshot.ranking);
    const layout = $derived(playerListLayout(ranking.length));
    const joinUrl = $derived(
        snapshot.joinAddress ? composeJoinUrl(snapshot.joinAddress, location) : null,
    );

    const rows: HTMLLIElement[] = [];

    // The rows show in the order of the previous ranking, then slide to their new place: in CSS
    // transforms only, smooth even on a Raspberry Pi. Once, as the ranking appears.
    onMount(() => {
        if (matchMedia('(prefers-reduced-motion: reduce)').matches) {
            return;
        }
        const from = previousIndexes(ranking);
        const places = rows.map((row) => row.getBoundingClientRect());
        rows.forEach((row, index) => {
            const start = places[from[index] ?? index];
            const end = places[index];
            if (!start || !end || (start.left === end.left && start.top === end.top)) {
                return;
            }
            const offset = `translate(${start.left - end.left}px, ${start.top - end.top}px)`;
            row.animate([{ transform: offset }, { transform: 'none' }], {
                duration: 1200,
                delay: 500,
                easing: 'ease-in-out',
                fill: 'backwards',
            });
        });
    });

    function moveText(gained: number): string {
        return gained > 0
            ? fill(fr.game.rankMove.up, { count: gained })
            : gained < 0
              ? fill(fr.game.rankMove.down, { count: -gained })
              : fr.game.rankMove.same;
    }
</script>

<main>
    <header>
        <div class="titles">
            <p class="progress">{roundText(fr.game.roundEnded, round)}</p>
            <h1>{fill(fr.game.rankingAfter, { number: round.number })}</h1>
        </div>
        {#if joinUrl}
            <!-- Registration stays open between two rounds: late arrivals still join here. -->
            <div class="join">
                <p>{fr.display.lateArrivals}</p>
                <div class="qr">
                    <QrCode text={joinUrl} label={fr.display.qrCodeLabel} />
                </div>
            </div>
        {/if}
    </header>
    <ol
        aria-label={fr.game.rankingLabel}
        style:--columns={layout.columns}
        style:--rows={Math.ceil(ranking.length / layout.columns)}
        style:--nickname-size={layout.fontSize}
    >
        {#each ranking as player, index (player.id)}
            {@const gained = placesGained(player)}
            <li class:disconnected={!player.isConnected} bind:this={rows[index]}>
                <span class="rank">{rankText(fr.game.rank, player.rank)}</span>
                <!-- Plain text interpolation: Svelte escapes it, so a nickname is never read as HTML. -->
                <span class="nickname">{player.nickname}</span>
                {#if !player.isConnected}
                    <!-- Dimmed and marked with an icon: never told apart by colour alone. -->
                    <ConnectionIcon connected={false} />
                    <span class="visually-hidden">({fr.display.disconnected})</span>
                {/if}
                {#if gained !== null}
                    <!-- An arrow and a number, never colour alone; a sentence for screen readers. -->
                    <span class="move">
                        <span aria-hidden="true">{moveText(gained)}</span>
                        <span class="visually-hidden">
                            {countText(
                                gained >= 0 ? fr.game.rankMoveLabel.up : fr.game.rankMoveLabel.down,
                                Math.abs(gained),
                            )}
                        </span>
                    </span>
                {/if}
                <span class="score">{countText(fr.game.points, player.score)}</span>
            </li>
        {/each}
    </ol>
</main>

<style>
    /* TVs may crop their edges (overscan): nothing essential within 5% of any border. */
    main {
        display: flex;
        flex-direction: column;
        gap: 3vh;
        height: 100vh;
        padding: 5vh 5vw;
        overflow: hidden;
    }

    header {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 3vw;
    }

    .titles {
        display: flex;
        flex-direction: column;
        gap: 1vh;
        min-width: 0;
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

    .join {
        display: flex;
        flex: 0 0 auto;
        align-items: center;
        gap: 1.5vw;
        max-width: 34vw;
        color: var(--color-text-muted);
        font-size: 2.4vh;
        text-align: right;
    }

    .qr {
        flex: 0 0 auto;
        width: 16vh;
    }

    /* Filled column by column, so that the ranks read top to bottom as on a podium list. */
    ol {
        display: grid;
        grid-template-rows: repeat(var(--rows), auto);
        grid-template-columns: repeat(var(--columns), minmax(0, 1fr));
        grid-auto-flow: column;
        align-content: start;
        gap: 0.8vh 2vw;
        /* A score stays close to its nickname, even in a single column. */
        max-width: calc(var(--columns) * 45vw);
        margin: 0;
        padding: 0;
        font-size: var(--nickname-size);
        line-height: 1.25;
        list-style: none;
    }

    li {
        display: flex;
        align-items: center;
        gap: 0.5em;
        min-width: 0;
        padding: 0.15em 0.5em;
        border-radius: var(--radius);
        background: var(--color-surface);
        font-weight: 700;
    }

    .rank {
        flex: 0 0 2.6em;
        color: var(--color-accent);
    }

    .nickname {
        flex: 1;
        min-width: 0;
        /* A nickname too wide for its column wraps rather than being cut. */
        overflow-wrap: anywhere;
    }

    .score {
        flex: 0 0 auto;
        white-space: nowrap;
    }

    .move {
        flex: 0 0 auto;
        color: var(--color-text-muted);
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
