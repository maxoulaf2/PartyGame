<script lang="ts">
    import Countdown from '../../shared/components/Countdown.svelte';
    import type { QuizDisplayView } from '../../shared/contracts';
    import { fill } from '../../shared/i18n/fill';
    import { fr } from '../../shared/i18n/fr';
    import type { DisplayViewProps } from '../../shared/modeViews';
    import ChoiceMarker from './ChoiceMarker.svelte';
    import { choiceColor } from './choiceTheme';

    let { view, round, clock }: DisplayViewProps<QuizDisplayView> = $props();

    // An image that cannot be loaded leaves the question on screen without it: never a broken
    // image on the TV.
    let failedImage = $state<string | null>(null);
    const image = $derived(view.imageUrl !== failedImage ? view.imageUrl : null);
</script>

<main>
    <header>
        <p class="round">{round.title}</p>
        <div class="status">
            <p class="progress">
                {fill(fr.modes.quiz.question, {
                    number: view.questionNumber,
                    count: view.questionCount,
                })}
            </p>
            {#if view.phase !== 'Presentation'}
                <!-- How many answered, never what: the choices stay secret until the reveal. -->
                <p class="answered">
                    {fill(fr.modes.quiz.answered, {
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
                        label={fr.modes.quiz.timeLeft}
                    />
                </p>
            {:else if view.phase === 'Locked'}
                <p class="time-up">{fr.modes.quiz.timeUp}</p>
            {/if}
        </div>
    </header>
    <div class="question">
        {#if image}
            <img
                src={image}
                alt={fr.modes.quiz.display.imageLabel}
                onerror={() => (failedImage = image)}
            />
        {/if}
        <h1>{view.text}</h1>
    </div>
    <ol class="choices" aria-label={fr.modes.quiz.choicesLabel}>
        {#each view.choices as choice (choice.letter)}
            <li style:--choice-color={choiceColor(choice.letter)}>
                <ChoiceMarker letter={choice.letter} />
                <span class="text">{choice.text}</span>
            </li>
        {/each}
    </ol>
</main>

<style>
    /* Read from 3 m on a 1080p TV, without scrolling: a question of 200 characters, its image
       and four choices of 80 characters fit at once. TVs may crop their edges (overscan):
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

    .question {
        display: flex;
        flex: 1 1 auto;
        align-items: center;
        gap: 3vw;
        min-height: 0;
    }

    img {
        flex: none;
        max-width: 35vw;
        max-height: 32vh;
        border-radius: var(--radius);
        object-fit: contain;
    }

    h1 {
        flex: 1 1 0;
        font-size: 3.25rem;
        line-height: 1.2;
        overflow-wrap: anywhere;
    }

    .choices {
        display: grid;
        grid-template-columns: 1fr 1fr;
        gap: 2vh 2vw;
        margin: 0;
        padding: 0;
        list-style: none;
    }

    li {
        display: flex;
        align-items: center;
        gap: 1.5vw;
        padding: 1.5vh 1.5vw;
        border-left: 0.6vw solid var(--choice-color);
        border-radius: var(--radius);
        background: var(--color-surface);
        line-height: 1.2;
    }

    .text {
        min-width: 0;
        overflow-wrap: anywhere;
    }
</style>
