<script lang="ts">
    import type { ServerClock } from '../connection/clockSync.svelte';
    import { secondsLeft, untilNextSecond } from '../countdown';

    interface Props {
        /** When the countdown ends, on the clock of the server, in ms since the Unix epoch. */
        closeAt: number;
        clock: ServerClock;
        /** Read by screen readers before the number, which alone shows. */
        label: string;
        /** The number in a sticker ring that empties as the time runs out, sized by the font. */
        ring?: boolean;
    }

    let { closeAt, clock, label, ring = false }: Props = $props();

    // Read again at each tick, and whenever the estimate of the clock changes.
    let ticks = $state(0);
    const seconds = $derived.by(() => {
        void ticks;
        return secondsLeft(closeAt, clock.serverNow());
    });

    // ponytail: the snapshot tells when the countdown ends, not how long it lasts, so the ring
    // starts full from the time first seen: a page reloaded midway starts it full again. Add the
    // duration to the views if that ever matters.
    let longest = 0;
    const left = $derived.by(() => {
        longest = Math.max(longest, seconds);
        return longest > 0 ? (seconds / longest) * 100 : 0;
    });

    // Ticks when a whole second of the server passes, the same instant on every screen, and stops
    // at 0: only the next snapshot ends the answers.
    $effect(() => {
        let timer: ReturnType<typeof setTimeout> | undefined;
        const schedule = () => {
            const wait = untilNextSecond(closeAt, clock.serverNow());
            timer = wait === null ? undefined : setTimeout(tick, wait);
        };
        const tick = () => {
            ticks++;
            schedule();
        };
        schedule();
        return () => clearTimeout(timer);
    });
</script>

<span class="countdown" class:ring role="timer" style:--left="{left}%">
    <span class="visually-hidden">{label}</span>
    <span class="seconds">{seconds}</span>
</span>

<style>
    .countdown {
        font-variant-numeric: tabular-nums;
        font-weight: 800;
    }

    .ring {
        display: inline-grid;
        place-items: center;
        width: 2.75em;
        height: 2.75em;
        border: var(--sticker-line) solid var(--color-ink);
        border-radius: 50%;
        background: conic-gradient(
            var(--color-accent) 0 var(--left),
            var(--color-night) var(--left) 100%
        );
        box-shadow: 0 0.15em 0 var(--color-ink);
        line-height: 1;
    }

    .ring .seconds {
        display: grid;
        place-items: center;
        width: 2em;
        height: 2em;
        border: var(--sticker-line) solid var(--color-ink);
        border-radius: 50%;
        background: var(--color-surface);
        color: var(--color-on-surface);
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
