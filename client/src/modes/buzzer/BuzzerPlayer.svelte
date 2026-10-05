<script lang="ts">
    import BuzzerButton from '../../shared/components/BuzzerButton.svelte';
    import type { BuzzerState } from '../../shared/buzzer.svelte';
    import type { BuzzerButtonState, BuzzerBuzz, BuzzerPlayerView } from '../../shared/contracts';
    import { countText } from '../../shared/i18n/countText';
    import { fill } from '../../shared/i18n/fill';
    import { fr } from '../../shared/i18n/fr';
    import { formatNumber } from '../../shared/i18n/numberText';
    import type { PlayerViewProps } from '../../shared/modeViews';

    // A buzzer only: the question is read on the TV screen, and the answer given out loud.
    let {
        view,
        round,
        score,
        clock,
        interactive,
        send,
        pending,
    }: PlayerViewProps<BuzzerPlayerView, BuzzerBuzz> = $props();

    const states: Readonly<Record<BuzzerButtonState, BuzzerState>> = {
        Closed: 'closed',
        Open: 'open',
        Buzzed: 'sent',
        Won: 'won',
        Lost: 'lost',
        Blocked: 'blocked',
    };

    // A buzz sent and not acknowledged yet shows as sent, even after a reload: the snapshot wins
    // once it tells otherwise.
    const state = $derived.by(() => {
        const buzzed = pending.some(
            (intent) =>
                intent.questionNumber === view.questionNumber && intent.opening === view.opening,
        );
        return view.buzzer === 'Open' && buzzed ? 'sent' : states[view.buzzer];
    });

    function buzz(pressedAt: number) {
        send({
            type: 'buzzer.buzz',
            roundId: round.roundId,
            questionNumber: view.questionNumber,
            opening: view.opening,
            pressedAt: Math.round(pressedAt),
        });
    }
</script>

<main>
    <p class="progress">
        {fill(fr.modes.buzzer.question, { number: view.questionNumber, count: view.questionCount })}
    </p>
    <BuzzerButton
        {state}
        opening={`${view.questionNumber}:${view.opening}`}
        {clock}
        {interactive}
        onbuzz={buzz}
    />
    <p class="status" role="status">
        {view.points !== null
            ? fill(fr.modes.buzzer.player.pointsEarned, { points: formatNumber(view.points) })
            : view.buzzer === 'Lost' && view.winner !== null
              ? fill(fr.modes.buzzer.hasHand, { nickname: view.winner })
              : ''}
    </p>
    {#if view.points !== null}
        <p class="score">{countText(fr.modes.buzzer.player.score, score)}</p>
    {/if}
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

    .score {
        color: var(--color-text-muted);
    }

    .status {
        min-height: 1.5em;
        font-size: 1.5rem;
        font-weight: 800;
        overflow-wrap: anywhere;
    }
</style>
