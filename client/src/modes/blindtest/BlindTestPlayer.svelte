<script lang="ts">
    import BuzzerButton from '../../shared/components/BuzzerButton.svelte';
    import type { BuzzerState } from '../../shared/buzzer.svelte';
    import type {
        BlindTestBuzz,
        BlindTestPlayerView,
        BuzzerButtonState,
    } from '../../shared/contracts';
    import { fill } from '../../shared/i18n/fill';
    import { fr } from '../../shared/i18n/fr';
    import type { PlayerViewProps } from '../../shared/modeViews';

    // A buzzer only: the music plays on the TV screen, and the answer is given out loud.
    let {
        view,
        round,
        clock,
        interactive,
        send,
        pending,
    }: PlayerViewProps<BlindTestPlayerView, BlindTestBuzz> = $props();

    const states: Readonly<Record<BuzzerButtonState, BuzzerState>> = {
        Closed: 'closed',
        Open: 'open',
        Buzzed: 'sent',
        Won: 'won',
        Lost: 'lost',
        Blocked: 'blocked',
    };

    // The buzzer opens as the music starts on the TV screen, a moment after the snapshot arrives.
    let started = $state(false);
    $effect(() => {
        const opensAt = view.opensAt;
        const wait = opensAt === null ? Infinity : opensAt - clock.serverNow();
        started = wait <= 0;
        if (wait > 0 && wait !== Infinity) {
            const timer = setTimeout(() => (started = true), wait);
            return () => clearTimeout(timer);
        }
    });

    // A buzz sent and not acknowledged yet shows as sent, even after a reload: the snapshot wins
    // once it tells otherwise.
    const buzzerState = $derived.by(() => {
        if (view.buzzer === 'Open' && !started) {
            return 'closed';
        }
        const buzzed = pending.some(
            (intent) => intent.trackNumber === view.trackNumber && intent.opening === view.opening,
        );
        return view.buzzer === 'Open' && buzzed ? 'sent' : states[view.buzzer];
    });

    const found = $derived(
        view.foundTitle && view.foundArtist
            ? fr.modes.blindtest.found.both
            : view.foundTitle
              ? fr.modes.blindtest.found.title
              : view.foundArtist
                ? fr.modes.blindtest.found.artist
                : null,
    );

    function buzz(pressedAt: number) {
        send({
            type: 'blindtest.buzz',
            roundId: round.roundId,
            trackNumber: view.trackNumber,
            opening: view.opening,
            pressedAt: Math.round(pressedAt),
        });
    }
</script>

<main>
    <p class="progress">
        {fill(fr.modes.blindtest.track, { number: view.trackNumber, count: view.trackCount })}
    </p>
    <BuzzerButton
        state={buzzerState}
        opening={`${view.trackNumber}:${view.opening}`}
        {clock}
        {interactive}
        onbuzz={buzz}
        labels={fr.modes.blindtest.buzzer}
    />
    <p class="status" role="status">
        {view.buzzer === 'Lost' && view.winner !== null
            ? fill(fr.modes.blindtest.hasHand, { nickname: view.winner })
            : (found ?? '')}
    </p>
</main>

<style>
    main {
        display: flex;
        flex-direction: column;
        gap: var(--space-m);
        min-height: 100vh;
        min-height: 100dvh;
        padding: var(--space-l) var(--space-m);
    }

    p {
        margin: 0;
        text-align: center;
    }

    .progress {
        color: var(--color-text-muted);
        font-weight: 700;
    }

    .status {
        min-height: 1.5em;
        font-size: 1.5rem;
        font-weight: 800;
        overflow-wrap: anywhere;
    }
</style>
