<script lang="ts">
    import type { ServerClock } from '../shared/connection/clockSync.svelte';
    import { fr } from '../shared/i18n/fr';
    import { flashLit } from './flash';

    interface Props {
        clock: ServerClock;
        onstop: () => void;
    }

    let { clock, onstop }: Props = $props();

    let lit = $state(false);

    $effect(() => {
        // Read from the clock at every frame, never counted: the flash cannot drift.
        let frame = requestAnimationFrame(function paint() {
            lit = flashLit(clock.serverNow());
            frame = requestAnimationFrame(paint);
        });
        return () => cancelAnimationFrame(frame);
    });
</script>

<!-- The whole screen stops the flash: a button covering it, for touch and keyboard alike. -->
<button
    type="button"
    class="flash"
    class:lit
    onclick={onstop}
    aria-label={fr.diagnostic.flash.stop}
>
    <span>{fr.diagnostic.flash.stop}</span>
</button>

<style>
    .flash {
        position: fixed;
        inset: 0;
        display: flex;
        align-items: flex-end;
        justify-content: center;
        padding: var(--space-l);
        border: none;
        background: #000;
        color: #666;
        font: inherit;
        touch-action: manipulation;
        cursor: pointer;
    }

    .flash.lit {
        background: #fff;
    }
</style>
