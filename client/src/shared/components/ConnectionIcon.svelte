<script lang="ts">
    interface Props {
        /** Whether the phone is connected: a barred Wi-Fi icon otherwise. */
        connected: boolean;
    }

    let { connected }: Props = $props();
</script>

<!-- Decorative: a text next to it tells the state as well, at least to screen readers. -->
<svg viewBox="0 0 24 24" aria-hidden="true">
    <path d="M2 8.5a15 15 0 0 1 20 0M5.5 12a10 10 0 0 1 13 0M9 15.5a5 5 0 0 1 6 0" />
    <circle cx="12" cy="19" r="1.2" />
    {#if !connected}
        <path d="M3 3l18 18" />
    {/if}
</svg>

<style>
    svg {
        flex: none;
        width: 1em;
        height: 1em;
        fill: currentColor;
        stroke: currentColor;
        stroke-linecap: round;
        stroke-width: 2;
    }

    path {
        fill: none;
    }
</style>
