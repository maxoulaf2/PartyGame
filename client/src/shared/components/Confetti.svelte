<script lang="ts">
    interface Props {
        /** Placed and sized for a TV screen, in pixels of its mockups (`--u`), else for a phone. */
        tv?: boolean;
        /** Taken in turn by the pieces: theme variables, light enough for the ink outline. */
        colors?: readonly string[];
    }

    let {
        tv = false,
        colors = [
            'var(--color-accent)',
            'var(--color-pink)',
            'var(--color-green)',
            'var(--color-blue)',
        ],
    }: Props = $props();

    // Left and top in %, size, corner radius (null for a disc) and rotation in degrees: fixed, so
    // that the pieces never move from one screen to the next.
    type Piece = readonly [number, number, number, number | null, number];
    const phone: readonly Piece[] = [
        [8, 6, 18, null, 20],
        [82, 4, 22, 5, 15],
        [90, 22, 14, null, 0],
        [4, 40, 12, 3, 30],
        [88, 55, 20, null, 0],
        [6, 78, 22, 5, -20],
        [78, 88, 16, null, 0],
        [48, 94, 12, 3, 45],
        [60, 2, 12, null, 0],
    ];
    const screen: readonly Piece[] = [
        [2, 8, 16, null, 0],
        [36, 4, 14, 4, 20],
        [97, 10, 18, null, 0],
        [96, 88, 16, 4, -15],
        [2, 90, 14, 4, 30],
        [40, 93, 12, null, 0],
    ];
    const pieces = $derived(tv ? screen : phone);
    const unit = (pixels: number) => (tv ? `calc(${pixels} * var(--u))` : `${pixels}px`);
</script>

<!-- Behind the content of a screen, which sets `position` and `isolation` for it. -->
<div class="confetti" aria-hidden="true">
    {#each pieces as [left, top, size, radius, rotation], index (index)}
        <span
            style:left="{left}%"
            style:top="{top}%"
            style:width={unit(size)}
            style:height={unit(size)}
            style:border-radius={radius === null ? '50%' : unit(radius)}
            style:transform="rotate({rotation}deg)"
            style:background={colors[index % colors.length]}
        ></span>
    {/each}
</div>

<style>
    .confetti {
        position: absolute;
        inset: 0;
        z-index: -1;
        overflow: hidden;
        pointer-events: none;
    }

    span {
        position: absolute;
        border: 2px solid var(--color-ink);
    }
</style>
