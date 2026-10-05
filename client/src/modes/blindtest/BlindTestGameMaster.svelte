<script lang="ts">
    import ConfirmDialog from '../../shared/components/ConfirmDialog.svelte';
    import type {
        BlindTestGameMasterView,
        BlindTestJudge,
        BlindTestNextTrack,
        BlindTestPlay,
        BlindTestRevealAnswer,
        BlindTestSkipTrack,
    } from '../../shared/contracts';
    import { fill } from '../../shared/i18n/fill';
    import { fr } from '../../shared/i18n/fr';
    import type { GameMasterViewProps } from '../../shared/modeViews';

    type Intent =
        | BlindTestPlay
        | BlindTestJudge
        | BlindTestRevealAnswer
        | BlindTestNextTrack
        | BlindTestSkipTrack;

    let { view, round, interactive, send }: GameMasterViewProps<BlindTestGameMasterView, Intent> =
        $props();

    let sending = $state(false);
    // The track whose skip awaits confirmation: the dialog goes once the round moves on.
    let skipping = $state<number | null>(null);
    const confirmingSkip = $derived(skipping === view.trackNumber);

    // What the game master ticked for the player who has the hand: forgotten once the buzzer
    // opens anew, so that the next winner starts from nothing.
    type Verdict = 'title' | 'artist' | 'nothing';
    let ticked = $state<{ opening: string; verdicts: Verdict[] }>({ opening: '', verdicts: [] });
    const opening = $derived(`${view.trackNumber}:${view.opening}`);
    const verdicts = $derived(ticked.opening === opening ? ticked.verdicts : []);
    const canFindArtist = $derived(view.artist !== null && view.artistFoundBy === null);

    function tick(verdict: Verdict) {
        const others = verdict === 'nothing' ? [] : verdicts.filter((v) => v !== 'nothing');
        const verdictsTicked = verdicts.includes(verdict)
            ? others.filter((v) => v !== verdict)
            : [...others, verdict];
        ticked = { opening, verdicts: verdictsTicked };
    }

    async function act(intent: Intent) {
        if (!interactive || sending) {
            return;
        }
        sending = true;
        // Each intent names its track, and a judgment the opening it judges: sent again, or by a
        // second console, it changes nothing.
        await send(intent);
        sending = false;
        skipping = null;
    }

    function step(
        type:
            | 'blindtest.play'
            | 'blindtest.revealAnswer'
            | 'blindtest.nextTrack'
            | 'blindtest.skipTrack',
    ) {
        void act({ type, roundId: round.roundId, trackNumber: view.trackNumber });
    }

    function judge() {
        void act({
            type: 'blindtest.judge',
            roundId: round.roundId,
            trackNumber: view.trackNumber,
            opening: view.opening,
            titleFound: verdicts.includes('title'),
            artistFound: verdicts.includes('artist'),
        });
    }
</script>

<div class="blindtest">
    <p class="progress">
        {fill(fr.modes.blindtest.track, { number: view.trackNumber, count: view.trackCount })}
    </p>
    <h3>{fill(fr.modes.blindtest.gm.title, { title: view.title })}</h3>
    <p class="artist">
        {view.artist !== null
            ? fill(fr.modes.blindtest.gm.artist, { artist: view.artist })
            : fr.modes.blindtest.gm.noArtist}
    </p>
    {#if view.titleFoundBy !== null}
        <p class="found">
            {fill(fr.modes.blindtest.titleFoundBy, { nickname: view.titleFoundBy })}
        </p>
    {/if}
    {#if view.artistFoundBy !== null}
        <p class="found">
            {fill(fr.modes.blindtest.artistFoundBy, { nickname: view.artistFoundBy })}
        </p>
    {/if}
    {#if view.phase === 'Answering' && view.winner !== null}
        <p class="winner" role="status">
            {fill(fr.modes.blindtest.hasHand, { nickname: view.winner })}
        </p>
    {:else if view.phase === 'Listening'}
        <p class="waiting" role="status">{fr.modes.blindtest.gm.waitingBuzz}</p>
    {:else if view.phase === 'Revealed' && view.titleFoundBy === null && view.artistFoundBy === null}
        <p class="waiting" role="status">{fr.modes.blindtest.nobodyFound}</p>
    {/if}
    {#if view.phase === 'Answering'}
        <div class="verdicts" role="group" aria-label={fr.modes.blindtest.gm.judge}>
            {#if view.titleFoundBy === null}
                <button
                    type="button"
                    class="toggle"
                    aria-pressed={verdicts.includes('title')}
                    disabled={!interactive || sending}
                    onclick={() => tick('title')}
                >
                    {fr.modes.blindtest.gm.titleFound}
                </button>
            {/if}
            {#if canFindArtist}
                <button
                    type="button"
                    class="toggle"
                    aria-pressed={verdicts.includes('artist')}
                    disabled={!interactive || sending}
                    onclick={() => tick('artist')}
                >
                    {fr.modes.blindtest.gm.artistFound}
                </button>
            {/if}
            <button
                type="button"
                class="toggle wrong"
                aria-pressed={verdicts.includes('nothing')}
                disabled={!interactive || sending}
                onclick={() => tick('nothing')}
            >
                {fr.modes.blindtest.gm.nothingFound}
            </button>
        </div>
    {/if}
    <div class="actions">
        {#if view.phase === 'Ready'}
            <button
                type="button"
                disabled={!interactive || sending}
                onclick={() => step('blindtest.play')}
            >
                {fr.modes.blindtest.gm.play}
            </button>
        {:else if view.phase === 'Answering'}
            <button
                type="button"
                disabled={!interactive || sending || verdicts.length === 0}
                onclick={judge}
            >
                {fr.modes.blindtest.gm.validate}
            </button>
        {:else if view.phase === 'Revealed'}
            <button
                type="button"
                disabled={!interactive || sending}
                onclick={() => step('blindtest.nextTrack')}
            >
                {view.trackNumber === view.trackCount
                    ? fr.modes.blindtest.gm.endRound
                    : fr.modes.blindtest.gm.nextTrack}
            </button>
        {/if}
        {#if view.phase === 'Listening' || view.phase === 'Answering'}
            <button
                type="button"
                class="secondary"
                disabled={!interactive || sending}
                onclick={() => step('blindtest.revealAnswer')}
            >
                {fr.modes.blindtest.gm.revealAnswer}
            </button>
        {/if}
        {#if view.phase !== 'Revealed'}
            <button
                type="button"
                class="secondary"
                disabled={!interactive || sending}
                onclick={() => (skipping = view.trackNumber)}
            >
                {fr.modes.blindtest.gm.skipTrack}
            </button>
        {/if}
    </div>
</div>

{#if confirmingSkip}
    <ConfirmDialog
        title={fill(fr.modes.blindtest.gm.skipConfirm.title, { number: view.trackNumber })}
        message={view.trackNumber === view.trackCount
            ? fr.modes.blindtest.gm.skipConfirm.lastMessage
            : fr.modes.blindtest.gm.skipConfirm.message}
        confirmLabel={fr.modes.blindtest.gm.skipConfirm.confirm}
        cancelLabel={fr.modes.blindtest.gm.skipConfirm.cancel}
        confirmDisabled={!interactive || sending}
        onconfirm={() => step('blindtest.skipTrack')}
        oncancel={() => (skipping = null)}
    />
{/if}

<style>
    .blindtest {
        display: flex;
        flex-direction: column;
        gap: var(--space-s);
    }

    h3,
    p {
        margin: 0;
    }

    .progress {
        font-weight: 700;
    }

    h3 {
        font-size: 1.25rem;
        color: var(--color-accent);
        overflow-wrap: anywhere;
    }

    .artist {
        font-weight: 700;
        overflow-wrap: anywhere;
    }

    .found {
        font-weight: 700;
        overflow-wrap: anywhere;
    }

    .winner {
        font-size: 1.5rem;
        font-weight: 800;
        overflow-wrap: anywhere;
    }

    .waiting {
        color: var(--color-text-muted);
    }

    .verdicts {
        display: flex;
        flex-wrap: wrap;
        gap: var(--space-s);
    }

    .actions {
        display: flex;
        flex-wrap: wrap;
        gap: var(--space-s);
        margin-top: var(--space-s);
    }

    button {
        min-height: var(--touch-target-min);
        padding: 0 var(--space-l);
        border: 2px solid var(--color-accent);
        border-radius: var(--radius);
        background: var(--color-accent);
        color: var(--color-bg);
        font: inherit;
        font-size: 1.125rem;
        font-weight: 700;
        cursor: pointer;
        touch-action: manipulation;
    }

    button.secondary {
        background: transparent;
        color: var(--color-accent);
    }

    /* Ticked or not, told by the check mark as well as by the colours. */
    button.toggle {
        background: transparent;
        color: var(--color-accent);
    }

    button.toggle[aria-pressed='true'] {
        background: var(--color-accent);
        color: var(--color-bg);
    }

    button.toggle[aria-pressed='true']::before {
        content: '✓ ';
    }

    button.toggle.wrong {
        border-style: dashed;
    }

    button:disabled {
        opacity: 0.4;
        cursor: default;
    }
</style>
