<script lang="ts">
    import { onDestroy } from 'svelte';
    import { excerptPlayer } from '../../shared/audio/excerptPlayer';
    import type { BlindTestDisplayView } from '../../shared/contracts';
    import { fill } from '../../shared/i18n/fill';
    import { fr } from '../../shared/i18n/fr';
    import type { DisplayViewProps } from '../../shared/modeViews';

    let { view, round, clock }: DisplayViewProps<BlindTestDisplayView> = $props();

    // At every snapshot, and at every clock synchronization, which the player reads: preloaded
    // while announced, played on time, paused while a player answers.
    const player = excerptPlayer();
    $effect(() => {
        player.play(view.playback, clock);
    });
    onDestroy(() => player.stop());
</script>

<main>
    <header>
        <p class="round">{round.title}</p>
    </header>
    <div class="track">
        <h1>
            {fill(fr.modes.blindtest.track, { number: view.trackNumber, count: view.trackCount })}
        </h1>
        {#if view.phase === 'Ready'}
            <p class="hint">{fr.modes.blindtest.display.ready}</p>
        {:else if view.phase === 'Listening'}
            <p class="hint listen">{fr.modes.blindtest.display.listen}</p>
        {/if}
    </div>
    {#if view.titleFoundBy !== null || view.artistFoundBy !== null}
        <ul class="found">
            {#if view.titleFoundBy !== null}
                <li>{fill(fr.modes.blindtest.titleFoundBy, { nickname: view.titleFoundBy })}</li>
            {/if}
            {#if view.artistFoundBy !== null}
                <li>{fill(fr.modes.blindtest.artistFoundBy, { nickname: view.artistFoundBy })}</li>
            {/if}
        </ul>
    {/if}
    {#if view.winner !== null}
        <!-- Plain text interpolation: Svelte escapes it, so a nickname is never read as HTML. -->
        <p class="winner" role="status">
            {fill(fr.modes.blindtest.hasHand, { nickname: view.winner })}
        </p>
    {/if}
</main>

<style>
    /* Read from 3 m on a 1080p TV. TVs may crop their edges (overscan): nothing essential within
       5% of any border. */
    main {
        display: flex;
        flex-direction: column;
        gap: 3vh;
        height: 100vh;
        padding: 6vh 6vw;
        overflow: hidden;
    }

    header {
        font-weight: 700;
    }

    h1,
    p {
        margin: 0;
    }

    .round {
        color: var(--color-accent);
        overflow-wrap: anywhere;
    }

    .track {
        display: flex;
        flex: 1 1 auto;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        gap: 3vh;
        text-align: center;
    }

    h1 {
        font-size: 6rem;
        font-weight: 800;
    }

    .hint {
        color: var(--color-text-muted);
        font-size: 3rem;
        font-weight: 700;
    }

    .listen {
        color: var(--color-accent);
    }

    .found {
        display: flex;
        flex-wrap: wrap;
        justify-content: center;
        gap: 1vh 4vw;
        margin: 0;
        padding: 0;
        list-style: none;
        font-size: 3rem;
        font-weight: 700;
        text-align: center;
        overflow-wrap: anywhere;
    }

    /* Who answers, readable from the back of the room. */
    .winner {
        padding: 2vh 2vw;
        border-radius: var(--radius);
        background: var(--color-accent);
        color: var(--color-bg);
        font-size: 5rem;
        font-weight: 800;
        text-align: center;
        overflow-wrap: anywhere;
    }
</style>
