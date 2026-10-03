<script lang="ts">
    import Countdown from '../../shared/components/Countdown.svelte';
    import type { QuizChoiceLetter, QuizDisplayView } from '../../shared/contracts';
    import { countText } from '../../shared/i18n/countText';
    import { fill } from '../../shared/i18n/fill';
    import { fr } from '../../shared/i18n/fr';
    import type { DisplayViewProps } from '../../shared/modeViews';
    import ChoiceMarker from './ChoiceMarker.svelte';
    import { choiceColor } from './choiceTheme';
    import CorrectMark from './CorrectMark.svelte';

    let { view, round, clock }: DisplayViewProps<QuizDisplayView> = $props();

    // An image that cannot be loaded leaves the question on screen without it: never a broken
    // image on the TV.
    let failedImage = $state<string | null>(null);
    // Once revealed, the room looks at who chose what: the image gives way to the nicknames.
    const image = $derived(
        view.reveal === null && view.imageUrl !== failedImage ? view.imageUrl : null,
    );

    /** The players who chose `letter`, in order of arrival, once revealed. */
    function chosenBy(letter: QuizChoiceLetter) {
        return view.reveal?.answers.filter((answer) => answer.choice === letter) ?? [];
    }

    const unanswered = $derived(
        view.reveal?.answers.filter((answer) => answer.choice === null) ?? [],
    );

    const letters: readonly QuizChoiceLetter[] = ['A', 'B', 'C', 'D'];
    // The choices the game master has not shown yet keep their room, so that nothing moves on
    // screen as they show one by one.
    const hiddenLetters = $derived(letters.slice(view.choices.length, view.choiceCount));
</script>

<main class:revealed={view.reveal !== null}>
    <header>
        <p class="round">{round.title}</p>
        <div class="status">
            <p class="progress">
                {fill(fr.modes.quiz.question, {
                    number: view.questionNumber,
                    count: view.questionCount,
                })}
            </p>
            {#if view.phase !== 'Revealed' && view.participantCount > 0}
                <!-- How many answered, never what: the choices stay secret until the reveal. The
                     answers open with the first choice shown. -->
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
                <p class="time-up">
                    {view.answeredCount === view.participantCount
                        ? fr.modes.quiz.allAnswered
                        : fr.modes.quiz.timeUp}
                </p>
            {/if}
        </div>
    </header>
    <div class="question">
        {#if view.text === null}
            <!-- The game master reads the question out first: it shows right after. -->
            <p class="upcoming">
                {fill(fr.modes.quiz.display.upcoming, { number: view.questionNumber })}
            </p>
        {:else}
            {#if image}
                <img
                    src={image}
                    alt={fr.modes.quiz.display.imageLabel}
                    onerror={() => (failedImage = image)}
                />
            {/if}
            <h1>{view.text}</h1>
        {/if}
    </div>
    <ol class="choices" aria-label={fr.modes.quiz.choicesLabel}>
        {#each view.choices as choice (choice.letter)}
            {@const correct = view.reveal?.correctChoice === choice.letter}
            <li
                class:correct
                class:wrong={view.reveal !== null && !correct}
                style:--choice-color={choiceColor(choice.letter)}
            >
                <div class="choice">
                    <ChoiceMarker letter={choice.letter} />
                    <span class="text">{choice.text}</span>
                    {#if correct}
                        <span class="mark"><CorrectMark /></span>
                    {/if}
                </div>
                {#if view.reveal !== null}
                    {@const players = chosenBy(choice.letter)}
                    <div class="chosen-by">
                        <span class="count"
                            >{countText(fr.modes.quiz.choiceAnswers, players.length)}</span
                        >
                        {#if players.length > 0}
                            <ul
                                class="nicknames"
                                aria-label={fill(fr.modes.quiz.display.choicePlayersLabel, {
                                    letter: choice.letter,
                                })}
                            >
                                {#each players as player (player.playerId)}
                                    <!-- Plain text interpolation: Svelte escapes it, so a nickname is never read as HTML. -->
                                    <li>{player.nickname}</li>
                                {/each}
                            </ul>
                        {/if}
                    </div>
                {/if}
            </li>
        {/each}
        {#each hiddenLetters as letter (letter)}
            <li class="hidden-choice" aria-hidden="true">
                <div class="choice">
                    <ChoiceMarker {letter} />
                    <span class="text">&nbsp;</span>
                </div>
            </li>
        {/each}
    </ol>
    {#if unanswered.length > 0}
        <div class="unanswered">
            <span class="count">{fr.modes.quiz.display.unanswered}</span>
            <ul class="nicknames" aria-label={fr.modes.quiz.display.unanswered}>
                {#each unanswered as player (player.playerId)}
                    <li>{player.nickname}</li>
                {/each}
            </ul>
        </div>
    {/if}
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

    .choices > li {
        display: flex;
        flex-direction: column;
        gap: 1vh;
        padding: 1.5vh 1.5vw;
        border-left: 0.6vw solid var(--choice-color);
        border-radius: var(--radius);
        background: var(--color-surface);
        line-height: 1.2;
    }

    .choice {
        display: flex;
        align-items: center;
        gap: 1.5vw;
    }

    .text {
        min-width: 0;
        overflow-wrap: anywhere;
    }

    .hidden-choice {
        visibility: hidden;
    }

    /* The question, then each choice, shows as the game master reads it out. */
    @media (prefers-reduced-motion: no-preference) {
        h1,
        img,
        .choices > li {
            animation: appear 0.4s ease-out;
        }
    }

    /* A fade alone: nothing moves, so that nothing ever crosses the edges of the screen. */
    @keyframes appear {
        from {
            opacity: 0;
        }
    }

    /* Once revealed, the question has been read: it makes room for the nicknames, and each
       choice takes the whole width, its text on the left, who chose it on the right. A question
       of 200 characters, four choices of 80 and 20 nicknames of 16 under the same choice fit. */
    .revealed {
        gap: 1.5vh;
    }

    .revealed .question {
        flex: none;
    }

    .revealed h1 {
        font-size: 2.25rem;
    }

    .revealed .choices {
        grid-template-columns: 1fr;
        gap: 1.5vh;
    }

    .revealed .choices > li {
        flex-direction: row;
        align-items: flex-start;
        gap: 2vw;
        padding: 0.6vh 1.5vw;
    }

    .revealed .choice {
        flex: 0 0 34%;
        flex-wrap: wrap;
        gap: 0.5vh 1vw;
        font-size: 1.9rem;
        line-height: 1.15;
    }

    .revealed .text {
        flex: 1 1 0;
    }

    /* On a line of its own, so that the text of the correct choice keeps its width. */
    .mark {
        flex-basis: 100%;
    }

    /* The correct choice stands out by an icon, a label and a frame; the others fade, their
       nicknames staying readable. */
    .correct {
        outline: 0.3vw solid var(--color-text);
    }

    .wrong .choice {
        opacity: 0.45;
    }

    /* The count, then the nicknames below it, over the whole width left: 20 nicknames of 16
       characters fit under a single choice at a size readable from 3 m. */
    .chosen-by,
    .unanswered {
        display: flex;
        flex: 1 1 0;
        flex-direction: column;
        gap: 0.4vh;
        min-width: 0;
        font-size: 1.9rem;
        line-height: 1.1;
    }

    .count {
        color: var(--color-text-muted);
        font-weight: 700;
    }

    .nicknames {
        display: flex;
        flex-wrap: wrap;
        gap: 0.4vh 0.6vw;
        margin: 0;
        padding: 0;
        list-style: none;
    }

    .nicknames li {
        min-width: 0;
        padding: 0 0.3em;
        border-radius: var(--radius);
        background: var(--color-bg);
        font-weight: 700;
        overflow-wrap: anywhere;
    }

    .unanswered {
        flex: none;
        padding: 0 1.5vw;
    }
</style>
