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

<!-- Beyond 6 rows, the stickers slim down so that 20 long nicknames still fit. -->
<main class:dense={ranking.length > 6 * layout.columns}>
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
                <!-- Told by its written rank, never by the color of its badge alone. -->
                <span class="rank" data-rank={player.rank}
                    >{rankText(fr.game.rank, player.rank)}</span
                >
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
        gap: calc(20 * var(--u));
        height: 100vh;
        padding: 5vh 5vw;
        overflow: hidden;
        background: var(--color-blue);
        color: var(--color-ink);
    }

    header {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: calc(28 * var(--u));
    }

    .titles {
        display: flex;
        flex-direction: column;
        gap: calc(6 * var(--u));
        min-width: 0;
    }

    h1,
    p {
        margin: 0;
    }

    .progress {
        align-self: flex-start;
        padding: calc(4 * var(--u)) calc(14 * var(--u));
        border-radius: 999px;
        background: var(--color-ink);
        color: var(--color-surface);
        font-size: calc(16 * var(--u));
        font-weight: 700;
    }

    h1 {
        color: var(--color-surface);
        font-size: calc(40 * var(--u));
        font-weight: 800;
        line-height: 1.05;
        letter-spacing: -0.02em;
        -webkit-text-stroke: calc(1.5 * var(--u)) var(--color-ink);
        text-shadow: 0 calc(4 * var(--u)) 0 var(--color-ink);
    }

    .join {
        display: flex;
        flex: 0 0 auto;
        align-items: center;
        gap: calc(12 * var(--u));
        max-width: 34vw;
        padding: calc(8 * var(--u)) calc(8 * var(--u)) calc(8 * var(--u)) calc(14 * var(--u));
        border: var(--sticker-line) solid var(--color-ink);
        border-radius: calc(18 * var(--u));
        background: var(--color-surface);
        box-shadow: 0 calc(5 * var(--u)) 0 var(--color-ink);
        color: var(--color-on-surface);
        transform: rotate(2deg);
    }

    .join p {
        max-width: calc(120 * var(--u));
        font-size: calc(14 * var(--u));
        font-weight: 700;
        line-height: 1.2;
        text-align: right;
    }

    .qr {
        flex: 0 0 auto;
        width: calc(80 * var(--u));
        overflow: hidden;
        border: calc(2 * var(--u)) solid var(--color-ink);
        border-radius: calc(10 * var(--u));
    }

    /* Filled column by column, so that the ranks read top to bottom as on a podium list. */
    ol {
        display: grid;
        grid-template-rows: repeat(var(--rows), auto);
        grid-template-columns: repeat(var(--columns), minmax(0, 1fr));
        grid-auto-flow: column;
        align-content: start;
        gap: calc(12 * var(--u)) calc(22 * var(--u));
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
        gap: 0.55em;
        min-width: 0;
        padding: 0.27em 0.64em 0.27em 0.27em;
        border: var(--sticker-line) solid var(--color-ink);
        border-radius: calc(16 * var(--u));
        background: var(--color-surface);
        box-shadow: 0 calc(5 * var(--u)) 0 var(--color-ink);
        font-weight: 800;
    }

    .rank {
        display: grid;
        flex: 0 0 2.6em;
        place-items: center;
        height: 2em;
        border: calc(2.5 * var(--u)) solid var(--color-ink);
        border-radius: calc(12 * var(--u));
        background: var(--color-sand);
        font-size: 0.9em;
    }

    .rank[data-rank='1'] {
        background: var(--color-accent);
    }

    .rank[data-rank='2'] {
        background: var(--color-blue);
    }

    .rank[data-rank='3'] {
        background: var(--color-pink);
    }

    .nickname {
        flex: 1;
        min-width: 0;
        /* A nickname too wide for its column wraps rather than being cut. */
        overflow-wrap: anywhere;
    }

    .score {
        flex: 0 0 auto;
        font-size: 0.82em;
        font-weight: 700;
        white-space: nowrap;
    }

    .move {
        flex: 0 0 auto;
        color: var(--color-on-surface-muted);
        font-size: 0.82em;
        white-space: nowrap;
    }

    .disconnected .nickname {
        opacity: 0.45;
    }

    .dense {
        --sticker-line: calc(2 * var(--u));
    }

    .dense ol {
        gap: 0.8vh 2vw;
    }

    .dense li {
        padding: 0.1em 0.5em 0.1em 0.1em;
        box-shadow: 0 calc(3 * var(--u)) 0 var(--color-ink);
    }

    .dense .rank {
        height: 1.3em;
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
