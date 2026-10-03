<script lang="ts">
    import type { ServerClock } from '../connection/clockSync.svelte';
    import { secondsLeft, untilNextSecond } from '../countdown';

    interface Props {
        /** When the countdown ends, on the clock of the server, in ms since the Unix epoch. */
        closeAt: number;
        clock: ServerClock;
        /** Read by screen readers before the number, which alone shows. */
        label: string;
    }

    let { closeAt, clock, label }: Props = $props();

    // Read again at each tick, and whenever the estimate of the clock changes.
    let ticks = $state(0);
    const seconds = $derived.by(() => {
        void ticks;
        return secondsLeft(closeAt, clock.serverNow());
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

<span class="countdown" role="timer">
    <span class="visually-hidden">{label}</span>
    <span class="seconds">{seconds}</span>
</span>

<style>
    .countdown {
        font-variant-numeric: tabular-nums;
        font-weight: 800;
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
