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

    let { view, round, clock, reportMediaFailure }: DisplayViewProps<QuizDisplayView> = $props();

    // An image that cannot be loaded leaves the question on screen without it: never a broken
    // image on the TV, and the game master hears of it.
    let failedImage = $state<string | null>(null);
    // Once revealed, the room looks at who chose what: the image gives way to the nicknames.
    const image = $derived(
        view.reveal === null && view.imageUrl !== failedImage ? view.imageUrl : null,
    );

    function imageFailed(url: string) {
        failedImage = url;
        reportMediaFailure(url);
    }

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
    // Long texts take a smaller type, so that a question of 200 characters, its image and four
    // choices of 80 characters still fit at once.
    const longQuestion = $derived((view.text?.length ?? 0) > 80);
    const longChoices = $derived(view.choices.some((choice) => choice.text.length > 40));
    // A reveal of long texts, or of many players, keeps the smaller type of the nicknames as well.
    const crowded = $derived(
        view.reveal !== null && (longQuestion || longChoices || view.reveal.answers.length > 12),
    );
</script>

<main class:revealed={view.reveal !== null} class:crowded>
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
                        ring
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
    <div class="question" class:long={longQuestion}>
        {#if view.text === null}
            <!-- The game master reads the question out first: it shows right after. -->
            <p class="upcoming">
                {fill(fr.modes.quiz.display.upcoming, { number: view.questionNumber })}
            </p>
        {:else}
            {#if image}
                {@const src = image}
                <img
                    {src}
                    alt={fr.modes.quiz.display.imageLabel}
                    onerror={() => imageFailed(src)}
                />
            {/if}
            <h1>{view.text}</h1>
        {/if}
    </div>
    <ol class="choices" class:long={longChoices} aria-label={fr.modes.quiz.choicesLabel}>
        {#each view.choices as choice (choice.letter)}
            {@const correct = view.reveal?.correctChoice === choice.letter}
            <li
                class:correct
                class:wrong={view.reveal !== null && !correct}
                style:--choice-color={choiceColor(choice.letter)}
            >
                <div class="choice">
                    <!-- The letter and the shape of the choice, on a sticker of their own. -->
                    <span class="badge">
                        <ChoiceMarker letter={choice.letter} color="var(--color-ink)" />
                    </span>
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
                    <span class="badge"><ChoiceMarker {letter} color="var(--color-ink)" /></span>
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
        gap: calc(18 * var(--u));
        height: 100vh;
        padding: 6vh 6vw;
        overflow: hidden;
        color: var(--color-ink);
    }

    header {
        display: flex;
        justify-content: space-between;
        align-items: center;
        gap: calc(20 * var(--u));
    }

    h1,
    p {
        margin: 0;
    }

    .round {
        min-width: 0;
        padding: calc(5 * var(--u)) calc(16 * var(--u));
        border: var(--sticker-line) solid var(--color-ink);
        border-radius: 999px;
        background: var(--color-accent);
        box-shadow: 0 calc(4 * var(--u)) 0 var(--color-ink);
        font-size: calc(19 * var(--u));
        font-weight: 800;
        overflow-wrap: anywhere;
        transform: rotate(-2deg);
    }

    .status {
        display: flex;
        flex: none;
        align-items: center;
        gap: calc(12 * var(--u));
        font-size: calc(17 * var(--u));
    }

    .progress,
    .time-up {
        padding: calc(5 * var(--u)) calc(14 * var(--u));
        border: var(--sticker-line) solid var(--color-ink);
        border-radius: 999px;
        background: var(--color-surface);
        font-weight: 800;
    }

    .time-up {
        background: var(--color-accent);
    }

    .answered {
        padding: calc(8 * var(--u)) calc(14 * var(--u));
        border-radius: 999px;
        background: var(--color-ink);
        color: var(--color-surface);
        font-weight: 700;
    }

    /* The seconds left, readable from the back of the room. */
    .countdown {
        font-size: calc(26 * var(--u));
    }

    .question {
        display: flex;
        flex: 1 1 auto;
        align-items: center;
        gap: 3vw;
        min-height: 0;
        padding: calc(20 * var(--u)) calc(30 * var(--u));
        border: var(--sticker-line) solid var(--color-ink);
        border-radius: calc(26 * var(--u));
        background: var(--color-surface);
        box-shadow: 0 calc(8 * var(--u)) 0 var(--color-ink);
    }

    .upcoming {
        flex: 1 1 0;
        color: var(--color-on-surface-muted);
        font-size: calc(48 * var(--u));
        font-weight: 800;
        text-align: center;
    }

    img {
        flex: none;
        max-width: 35vw;
        max-height: 32vh;
        border-radius: calc(14 * var(--u));
        object-fit: contain;
    }

    h1 {
        flex: 1 1 0;
        font-size: calc(38 * var(--u));
        font-weight: 800;
        line-height: 1.1;
        letter-spacing: -0.02em;
        overflow-wrap: anywhere;
        text-wrap: pretty;
    }

    .question.long {
        padding: calc(10 * var(--u)) calc(20 * var(--u));
    }

    .long h1 {
        font-size: calc(26 * var(--u));
        line-height: 1.2;
    }

    .choices {
        display: grid;
        grid-template-columns: 1fr 1fr;
        gap: calc(14 * var(--u)) calc(18 * var(--u));
        margin: 0;
        padding: 0;
        font-size: calc(26 * var(--u));
        list-style: none;
    }

    .choices.long {
        font-size: calc(20 * var(--u));
    }

    .choices > li {
        display: flex;
        flex-direction: column;
        gap: 1vh;
        padding: calc(10 * var(--u)) calc(16 * var(--u));
        border: var(--sticker-line) solid var(--color-ink);
        border-radius: calc(18 * var(--u));
        background: var(--choice-color);
        box-shadow: 0 calc(5 * var(--u)) 0 var(--color-ink);
        font-weight: 800;
        line-height: 1.2;
    }

    .choice {
        display: flex;
        align-items: center;
        gap: calc(14 * var(--u));
    }

    .badge {
        display: flex;
        flex: none;
        padding: calc(4 * var(--u)) calc(10 * var(--u)) calc(4 * var(--u)) calc(8 * var(--u));
        border: calc(2.5 * var(--u)) solid var(--color-ink);
        border-radius: calc(12 * var(--u));
        background: var(--color-surface);
        font-size: calc(22 * var(--u));
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

    /* The correct choice, the moment it is revealed. */
    @media (prefers-reduced-motion: no-preference) {
        .choices > li.correct {
            animation: reveal-highlight 0.6s ease-out;
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
        gap: calc(11 * var(--u));
    }

    .revealed .question {
        flex: none;
        margin: calc(2 * var(--u)) 0 calc(4 * var(--u));
        padding: 0;
        border: none;
        background: none;
        box-shadow: none;
        color: var(--color-text);
    }

    .revealed h1 {
        font-size: calc(24 * var(--u));
        line-height: 1.15;
    }

    .revealed .choices {
        grid-template-columns: 1fr;
        gap: calc(11 * var(--u));
    }

    .revealed .choices > li {
        flex-direction: row;
        align-items: flex-start;
        gap: calc(18 * var(--u));
        padding: calc(8 * var(--u)) calc(14 * var(--u));
        border-radius: calc(16 * var(--u));
        background: var(--color-violet-light);
        box-shadow: none;
    }

    .revealed .choice {
        flex: 0 0 34%;
        flex-wrap: wrap;
        gap: calc(4 * var(--u)) calc(10 * var(--u));
        font-size: calc(20 * var(--u));
        line-height: 1.15;
    }

    .revealed .badge {
        padding: calc(2 * var(--u)) calc(9 * var(--u)) calc(2 * var(--u)) calc(7 * var(--u));
        border-radius: calc(10 * var(--u));
        background: var(--choice-color);
        font-size: inherit;
    }

    .revealed .text {
        flex: 1 1 0;
    }

    /* On a line of its own, so that the text of the correct choice keeps its width. */
    .mark {
        flex-basis: 100%;
        color: var(--color-correct-text);
        font-size: calc(15 * var(--u));
    }

    /* The correct choice stands out by an icon, a label, a cream sticker and its drop shadow; the
       others fade, their nicknames staying readable. */
    .revealed .choices > li.correct {
        background: var(--color-surface);
        box-shadow: 0 calc(5 * var(--u)) 0 var(--color-ink);
    }

    .wrong .choice {
        opacity: 0.6;
    }

    /* The count, then the nicknames below it, over the whole width left: 20 nicknames of 16
       characters fit under a single choice at a size readable from 3 m. */
    .chosen-by {
        display: flex;
        flex: 1 1 0;
        flex-direction: column;
        gap: calc(4 * var(--u));
        min-width: 0;
        font-size: calc(17 * var(--u));
        line-height: 1.1;
    }

    .count {
        font-weight: 700;
    }

    .correct .count {
        color: var(--color-on-surface-muted);
    }

    .nicknames {
        display: flex;
        flex-wrap: wrap;
        gap: calc(5 * var(--u));
        margin: 0;
        padding: 0;
        list-style: none;
    }

    .nicknames li {
        min-width: 0;
        padding: 0 calc(7 * var(--u));
        border: calc(2 * var(--u)) solid var(--color-ink);
        border-radius: calc(8 * var(--u));
        background: var(--color-white);
        font-weight: 800;
        overflow-wrap: anywhere;
    }

    .unanswered {
        display: flex;
        flex-wrap: wrap;
        align-items: center;
        gap: calc(10 * var(--u));
        padding: 0 calc(14 * var(--u));
        color: var(--color-text);
        font-size: calc(17 * var(--u));
        line-height: 1.1;
    }

    .unanswered .nicknames li {
        background: var(--color-night);
        color: var(--color-text);
    }

    /* Thinner outlines, and those of the nicknames drawn inside them, taking no room. */
    .crowded {
        --sticker-line: calc(1.5 * var(--u));
        gap: 1vh;
    }

    .crowded .question {
        margin: 0;
    }

    .crowded h1 {
        font-size: calc(18 * var(--u));
    }

    .crowded .progress,
    .crowded .round {
        padding-block: calc(2 * var(--u));
    }

    .crowded .choice,
    .crowded .chosen-by,
    .crowded .unanswered {
        font-size: calc(15.2 * var(--u));
    }

    .crowded .choices {
        gap: 1vh;
    }

    .crowded .choices > li {
        padding: calc(3 * var(--u)) calc(12 * var(--u));
    }

    .crowded .nicknames {
        gap: 0.4vh 0.6vw;
        line-height: 1;
    }

    .crowded .nicknames li {
        padding: calc(1 * var(--u)) calc(5 * var(--u));
        border: none;
        outline: calc(1.5 * var(--u)) solid var(--color-ink);
        outline-offset: calc(-1.5 * var(--u));
    }
</style>
