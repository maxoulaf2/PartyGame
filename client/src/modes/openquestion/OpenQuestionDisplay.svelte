<script lang="ts">
    import Countdown from '../../shared/components/Countdown.svelte';
    import type { OpenQuestionDisplayView } from '../../shared/contracts';
    import { fill } from '../../shared/i18n/fill';
    import { fr } from '../../shared/i18n/fr';
    import type { DisplayViewProps } from '../../shared/modeViews';

    let { view, round, clock, reportMediaFailure }: DisplayViewProps<OpenQuestionDisplayView> =
        $props();

    // An image that cannot be loaded leaves the question on screen without it: never a broken
    // image on the TV, and the game master hears of it.
    let failedImage = $state<string | null>(null);
    const image = $derived(view.imageUrl !== failedImage ? view.imageUrl : null);
    const locked = $derived(view.phase === 'Locked' || view.phase === 'Judged');

    function imageFailed(url: string) {
        failedImage = url;
        reportMediaFailure(url);
    }
</script>

<main>
    <header>
        <p class="round">{round.title}</p>
        <div class="status">
            <p class="progress">
                {fill(fr.modes.openquestion.question, {
                    number: view.questionNumber,
                    count: view.questionCount,
                })}
            </p>
            {#if view.participantCount > 0}
                <!-- How many answered, never what. -->
                <p class="answered">
                    {fill(fr.modes.openquestion.answered, {
                        answered: view.answeredCount,
                        participants: view.participantCount,
                    })}
                </p>
            {/if}
            {#if view.answersCloseAt !== null}
                <p class="countdown">
                    <Countdown
                        closeAt={view.answersCloseAt}
                        {clock}
                        label={fr.modes.openquestion.timeLeft}
                    />
                </p>
            {:else if locked}
                <p class="time-up">
                    {view.answeredCount === view.participantCount
                        ? fr.modes.openquestion.allAnswered
                        : fr.modes.openquestion.timeUp}
                </p>
            {/if}
        </div>
    </header>
    <div class="question">
        {#if view.text === null}
            <!-- The game master reads the question out first: it shows right after. -->
            <p class="upcoming">
                {fill(fr.modes.openquestion.display.upcoming, { number: view.questionNumber })}
            </p>
        {:else}
            {#if image}
                {@const src = image}
                <img
                    {src}
                    alt={fr.modes.openquestion.display.imageLabel}
                    onerror={() => imageFailed(src)}
                />
            {/if}
            <h1>{view.text}</h1>
        {/if}
    </div>
    {#if locked && view.answeredCount > 0}
        <!-- Neither verdict nor answer before the reveal: only that the game master checks them. -->
        <p class="checking">{fr.modes.openquestion.display.checking}</p>
    {/if}
</main>

<style>
    /* Read from 3 m on a 1080p TV, without scrolling. TVs may crop their edges (overscan):
       nothing essential within 5% of any border. */
    main {
        display: flex;
        flex-direction: column;
        gap: 3vh;
        height: 100vh;
        padding: 6vh 6vw;
        overflow: hidden;
    }

    header {
        display: flex;
        justify-content: space-between;
        align-items: center;
        gap: 4vw;
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

    .status {
        display: flex;
        flex: none;
        align-items: center;
        gap: 3vw;
    }

    .progress,
    .answered {
        color: var(--color-text-muted);
    }

    /* The seconds left, readable from the back of the room. */
    .countdown {
        min-width: 2.5ch;
        color: var(--color-accent);
        font-size: 3.5rem;
        line-height: 1;
        text-align: right;
    }

    .time-up {
        color: var(--color-accent);
        font-size: 2rem;
    }

    .checking {
        color: var(--color-text-muted);
        font-size: 2.5rem;
        font-weight: 700;
        text-align: center;
    }

    .question {
        display: flex;
        flex: 1 1 auto;
        align-items: center;
        gap: 3vw;
        min-height: 0;
    }

    .upcoming {
        flex: 1 1 0;
        color: var(--color-text-muted);
        font-size: 5rem;
        font-weight: 800;
        text-align: center;
    }

    img {
        flex: none;
        max-width: 40vw;
        max-height: 60vh;
        border-radius: var(--radius);
        object-fit: contain;
    }

    h1 {
        flex: 1 1 0;
        font-size: 4rem;
        line-height: 1.2;
        overflow-wrap: anywhere;
    }

    /* A fade alone: nothing moves, so that nothing ever crosses the edges of the screen. */
    @media (prefers-reduced-motion: no-preference) {
        h1,
        img {
            animation: appear 0.4s ease-out;
        }
    }

    @keyframes appear {
        from {
            opacity: 0;
        }
    }
</style>
