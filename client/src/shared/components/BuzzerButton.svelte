<script lang="ts">
    import { BuzzerPresses, type BuzzerState } from '../buzzer.svelte';
    import type { ServerClock } from '../connection/clockSync.svelte';
    import { fr } from '../i18n/fr';

    interface Props {
        /** What the view of the mode decided from the snapshot. */
        state: BuzzerState;
        /** Names the current opening of the buzzer: a single buzz leaves per opening. */
        opening: string;
        clock: ServerClock;
        /** Whether the page shows the state of the server. */
        interactive: boolean;
        /** Sends the buzz, with the time of the press on the clock of the server. */
        onbuzz: (pressedAt: number) => void;
    }

    let { state, opening, clock, interactive, onbuzz }: Props = $props();

    // The props are read when a press buzzes, never captured once.
    const presses = new BuzzerPresses(
        {
            get synchronized() {
                return clock.synchronized;
            },
            toServerTime: (timestamp) => clock.toServerTime(timestamp),
        },
        (pressedAt) => onbuzz(pressedAt),
    );
    const shown = $derived(presses.shown(state, opening));
    const enabled = $derived(presses.enabled(state, opening, interactive));

    function onpointerdown() {
        // Taken here rather than once the finger lifts, nor once the message leaves.
        presses.press(state, opening, interactive, performance.now());
    }
</script>

<!-- No click: it only fires once the finger lifts. A long press opens no menu nor selects. -->
<button
    type="button"
    class="buzzer {shown}"
    disabled={!enabled}
    {onpointerdown}
    oncontextmenu={(event) => event.preventDefault()}
>
    <svg viewBox="0 0 24 24" aria-hidden="true">
        {#if shown === 'open'}
            <path d="M12 3a6 6 0 0 1 6 6v5l2 3H4l2-3V9a6 6 0 0 1 6-6zM10 20a2 2 0 0 0 4 0" />
        {:else if shown === 'sent'}
            <path d="M7 3h10M7 21h10M8 3c0 5 8 5 8 9s-8 4-8 9M16 3c0 5-8 5-8 9s8 4 8 9" />
        {:else if shown === 'won'}
            <path d="M4 12.5l5 5L20 6.5" />
        {:else if shown === 'blocked'}
            <path d="M5 5l14 14M12 3a9 9 0 1 0 0 18 9 9 0 1 0 0-18" />
        {:else}
            <path d="M6 12h12" />
        {/if}
    </svg>
    <span>{fr.buzzer[shown]}</span>
</button>

<style>
    /* Most of the screen, reachable by the thumb. Each state has its own icon and outline, never
       its color alone. */
    .buzzer {
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        gap: var(--space-m);
        width: min(85vw, 60vh);
        aspect-ratio: 1;
        margin: auto;
        padding: var(--space-l);
        border: 0.5rem solid transparent;
        border-radius: 50%;
        background: var(--color-surface);
        color: var(--color-text);
        font: inherit;
        font-size: 1.5rem;
        font-weight: 800;
        text-align: center;
        cursor: pointer;
        touch-action: manipulation;
        user-select: none;
        -webkit-user-select: none;
        -webkit-touch-callout: none;
        -webkit-tap-highlight-color: transparent;
    }

    svg {
        width: 4rem;
        height: 4rem;
        fill: none;
        stroke: currentColor;
        stroke-width: 2.5;
        stroke-linecap: round;
        stroke-linejoin: round;
    }

    .open {
        background: var(--color-accent);
        color: var(--color-bg);
        font-size: 2.5rem;
    }

    .open:active {
        transform: scale(0.97);
    }

    .sent {
        border-style: dashed;
        border-color: var(--color-accent);
    }

    /* A square: the shape alone tells this player has the hand. */
    .won {
        border-color: var(--color-accent);
        border-radius: var(--radius);
    }

    .closed,
    .lost,
    .blocked {
        color: var(--color-text-muted);
    }

    .buzzer:disabled {
        cursor: default;
    }
</style>
