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

<!-- Dark modules on a light background whatever the theme: phone cameras expect it. The quiet
     zone is part of the drawing, so it survives any surrounding background. -->
<svg
    viewBox="0 0 {shape.size} {shape.size}"
    role="img"
    aria-label={label}
    shape-rendering="crispEdges"
    data-qr-text={text}
>
    <rect width={shape.size} height={shape.size} fill="#fff" />
    <path d={shape.path} fill="#000" />
</svg>

<style>
    svg {
        display: block;
        width: 100%;
        height: auto;
    }
</style>
