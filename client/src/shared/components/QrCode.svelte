<script lang="ts">
    import { qrCodeShape } from '../qrCode';

    interface Props {
        /** Text to encode, usually the join URL. */
        text: string;
        /** Accessible name of the image. */
        label: string;
    }

    let { text, label }: Props = $props();

    const shape = $derived(qrCodeShape(text));
</script>

<!-- The quiet zone is part of the drawing, so it survives any surrounding background. -->
<svg
    viewBox="0 0 {shape.size} {shape.size}"
    role="img"
    aria-label={label}
    shape-rendering="crispEdges"
    data-qr-text={text}
>
    <rect width={shape.size} height={shape.size} />
    <path d={shape.path} />
</svg>

<style>
    svg {
        display: block;
        width: 100%;
        height: auto;
    }

    /* Dark modules on a light background whatever the theme: phone cameras expect it. */
    rect {
        fill: var(--color-white);
    }

    path {
        fill: var(--color-black);
    }
</style>
