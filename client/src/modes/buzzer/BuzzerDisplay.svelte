<script lang="ts">
    import type { BuzzerDisplayView } from '../../shared/contracts';
    import { fill } from '../../shared/i18n/fill';
    import { fr } from '../../shared/i18n/fr';
    import type { DisplayViewProps } from '../../shared/modeViews';

    let { view, round, reportMediaFailure }: DisplayViewProps<BuzzerDisplayView> = $props();

    // An image that cannot be loaded leaves the question on screen without it: never a broken
    // image on the TV, and the game master hears of it.
    let failedImage = $state<string | null>(null);
    const image = $derived(view.imageUrl !== failedImage ? view.imageUrl : null);

    function imageFailed(url: string) {
        failedImage = url;
        reportMediaFailure(url);
    }
</script>

<main>
    <header>
        <p class="round">{round.title}</p>
        <p class="progress">
            {fill(fr.modes.buzzer.question, {
                number: view.questionNumber,
                count: view.questionCount,
            })}
        </p>
    </header>
    <div class="question">
        {#if view.text === null}
            <!-- The question shows once the game master asks it, as the buzzer opens. -->
            <p class="upcoming">
                {fill(fr.modes.buzzer.display.upcoming, { number: view.questionNumber })}
            </p>
        {:else}
            {#if image}
                {@const src = image}
                <img
                    {src}
                    alt={fr.modes.buzzer.display.imageLabel}
                    onerror={() => imageFailed(src)}
                />
            {/if}
            <h1>{view.text}</h1>
        {/if}
    </div>
    {#if view.answer !== null}
        <div class="reveal" role="status">
            <p class="answer">{fill(fr.modes.buzzer.display.answer, { answer: view.answer })}</p>
            <p class="found-by">
                {view.foundBy !== null
                    ? fill(fr.modes.buzzer.foundBy, { nickname: view.foundBy })
                    : fr.modes.buzzer.nobodyFound}
            </p>
        </div>
    {:else if view.winner !== null}
        <!-- Plain text interpolation: Svelte escapes it, so a nickname is never read as HTML. -->
        <p class="winner" role="status">
            {fill(fr.modes.buzzer.hasHand, { nickname: view.winner })}
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

    .progress {
        color: var(--color-text-muted);
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
        max-width: 35vw;
        max-height: 45vh;
        border-radius: var(--radius);
        object-fit: contain;
    }

    h1 {
        flex: 1 1 0;
        font-size: 3.25rem;
        line-height: 1.2;
        overflow-wrap: anywhere;
    }

    .reveal {
        display: flex;
        flex-direction: column;
        gap: 1vh;
        text-align: center;
    }

    .answer {
        color: var(--color-accent);
        font-size: 4.5rem;
        font-weight: 800;
        overflow-wrap: anywhere;
    }

    .found-by {
        font-size: 3rem;
        font-weight: 700;
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
