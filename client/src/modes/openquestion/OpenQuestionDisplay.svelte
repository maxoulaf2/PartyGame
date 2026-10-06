<script lang="ts">
    import Countdown from '../../shared/components/Countdown.svelte';
    import type { OpenQuestionDisplayView } from '../../shared/contracts';
    import { fill } from '../../shared/i18n/fill';
    import { fr } from '../../shared/i18n/fr';
    import { formatSeconds } from '../../shared/i18n/numberText';
    import type { DisplayViewProps } from '../../shared/modeViews';

    let { view, round, clock, reportMediaFailure }: DisplayViewProps<OpenQuestionDisplayView> =
        $props();

    // An image that cannot be loaded leaves the question on screen without it: never a broken
    // image on the TV, and the game master hears of it.
    let failedImage = $state<string | null>(null);
    const image = $derived(view.imageUrl !== failedImage ? view.imageUrl : null);
    const locked = $derived(view.phase === 'Locked' || view.phase === 'Judged');

    // A tilt each, as the stickers of the lobby, taken in turn in the order of arrival.
    const tilts = [-2, 1.5, -1, 2, -1.5, 1];

    function imageFailed(url: string) {
        failedImage = url;
        reportMediaFailure(url);
    }
</script>

<main class:revealed={view.reveal !== null}>
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
            <!-- Once revealed, the answers take the room of the image. -->
            {#if image && view.reveal === null}
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
    {#if view.participants.length > 0}
        <!-- Who answered and how fast, never what: a ghost until then, a sticker slapped on once
             they did. Told by the time and its mark too, never by the color alone. -->
        <ul class="participants" aria-label={fr.modes.openquestion.display.participantsLabel}>
            {#each view.participants as participant, index (index)}
                {@const time = participant.answerMilliseconds}
                <li class:answered={time !== null} style:--tilt="{tilts[index % tilts.length]}deg">
                    <!-- Plain text interpolation: Svelte escapes nicknames. -->
                    <span class="nickname">{participant.nickname}</span>
                    {#if time === null}
                        <!-- The room of the time, kept so that no sticker moves when one answers. -->
                        <span class="time" aria-hidden="true">…</span>
                        <span class="visually-hidden">
                            ({fr.modes.openquestion.display.waitingMark})
                        </span>
                    {:else}
                        <span class="time">
                            <svg
                                viewBox="0 0 24 24"
                                width="1em"
                                height="1em"
                                role="img"
                                aria-label={fr.modes.openquestion.display.answeredMark}
                            >
                                <path d="M4 12.5l5 5L20 6.5" />
                            </svg>
                            {fill(fr.modes.openquestion.display.answerTime, {
                                seconds: formatSeconds(time),
                            })}
                        </span>
                    {/if}
                </li>
            {/each}
        </ul>
    {/if}
    {#if locked && view.answeredCount > 0}
        <!-- Neither verdict nor answer before the reveal: only that the game master checks them. -->
        <p class="checking">{fr.modes.openquestion.display.checking}</p>
    {/if}
    {#if view.reveal}
        <!-- Every answer received, grouped with its authors: the right ones, then the wrong ones,
             each told by an icon and not by the color alone. -->
        <section class="reveal">
            <p class="expected">
                <span class="label">{fr.modes.openquestion.display.expectedAnswer}</span>
                <span class="answer">{view.reveal.expectedAnswer}</span>
            </p>
            <!-- One grid, the right answers first: no row left half empty between the two. -->
            <ul aria-label={fr.modes.openquestion.display.answersLabel}>
                {#each view.reveal.groups as group, index (index)}
                    <li class:correct={group.correct}>
                        <span class="text">
                            <svg
                                viewBox="0 0 24 24"
                                width="1em"
                                height="1em"
                                role="img"
                                aria-label={group.correct
                                    ? fr.modes.openquestion.display.right
                                    : fr.modes.openquestion.display.wrong}
                            >
                                <path
                                    d={group.correct
                                        ? 'M4 12.5l5 5L20 6.5'
                                        : 'M6 6l12 12M18 6L6 18'}
                                />
                            </svg>
                            <!-- Plain text interpolation: Svelte escapes answers and nicknames. -->
                            {group.text}
                        </span>
                        <span class="authors">{group.nicknames.join(', ')}</span>
                    </li>
                {/each}
            </ul>
            {#if view.reveal.withoutAnswer.length > 0}
                <p class="unanswered">
                    <span class="label">{fr.modes.openquestion.display.unanswered}</span>
                    {view.reveal.withoutAnswer.join(', ')}
                </p>
            {/if}
        </section>
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
        /* Leaves its room to the participants below. */
        max-height: min(60vh, 100%);
        border-radius: var(--radius);
        object-fit: contain;
    }

    h1 {
        flex: 1 1 0;
        font-size: 4rem;
        line-height: 1.2;
        overflow-wrap: anywhere;
    }

    /* Up to 20 players fit below the question, from the back of the room. */
    .participants {
        display: flex;
        flex: none;
        flex-wrap: wrap;
        justify-content: center;
        gap: 1.5vh 1.2vw;
        margin: 0;
        padding: 0;
        list-style: none;
    }

    /* Not answered yet: a ghost of a sticker, dashed, flat on the ground. */
    .participants li {
        display: flex;
        align-items: center;
        gap: 0.4em;
        min-width: 0;
        padding: 0.15em 0.5em;
        border: var(--sticker-line) dashed var(--color-text-muted);
        border-radius: calc(14 * var(--u));
        color: var(--color-text-muted);
        font-size: 1.625rem;
        font-weight: 800;
        line-height: 1.25;
    }

    /* Answered: a cream sticker, outlined, tilted, with its hard drop shadow. The individual
       rotate property leaves transform to the animation. */
    .participants li.answered {
        border-style: solid;
        border-color: var(--color-ink);
        background: var(--color-surface);
        box-shadow: 0 calc(4 * var(--u)) 0 var(--color-ink);
        color: var(--color-ink);
        rotate: var(--tilt);
    }

    .nickname {
        min-width: 0;
        overflow-wrap: anywhere;
    }

    /* As wide as « 10,3 s » with its mark, answered or not. */
    .time {
        display: flex;
        flex: none;
        justify-content: center;
        align-items: center;
        gap: 0.2em;
        min-width: 4.6em;
        padding: 0 0.45em;
        border-radius: 999px;
        font-size: 0.85em;
        font-variant-numeric: tabular-nums;
    }

    .answered .time {
        background: var(--color-ink);
        color: var(--color-accent);
    }

    .time svg {
        fill: none;
        stroke: currentColor;
        stroke-width: 3.5;
        stroke-linecap: round;
        stroke-linejoin: round;
    }

    .visually-hidden {
        position: absolute;
        width: 1px;
        height: 1px;
        overflow: hidden;
        clip-path: inset(50%);
        white-space: nowrap;
    }

    /* Once revealed, the question gives its room to the answers: 20 players fit without
       scrolling, four answers a row. */
    main.revealed {
        gap: 2vh;
    }

    main.revealed .question {
        flex: none;
    }

    main.revealed h1 {
        display: -webkit-box;
        overflow: hidden;
        font-size: 2rem;
        -webkit-box-orient: vertical;
        -webkit-line-clamp: 2;
        line-clamp: 2;
    }

    .reveal {
        display: flex;
        flex: 1 1 auto;
        flex-direction: column;
        gap: 1.5vh;
        min-height: 0;
    }

    .expected {
        display: flex;
        align-items: baseline;
        gap: 2vw;
    }

    .label {
        color: var(--color-text-muted);
        font-weight: 700;
    }

    .expected .label {
        font-size: 1.75rem;
    }

    .expected .answer {
        color: var(--color-accent);
        font-size: 3rem;
        font-weight: 800;
        overflow-wrap: anywhere;
    }

    .reveal ul {
        display: grid;
        grid-template-columns: repeat(4, minmax(0, 1fr));
        gap: 1vh 1vw;
        margin: 0;
        padding: 0;
        list-style: none;
    }

    .reveal li {
        display: flex;
        flex-direction: column;
        padding: 0.3em 0.6em;
        border: var(--sticker-line) solid var(--color-ink);
        border-radius: var(--radius);
        background: var(--color-surface);
        color: var(--color-on-surface);
        font-size: 1.625rem;
        line-height: 1.2;
    }

    /* Told by its icon as well, never by its color alone. */
    .reveal li.correct {
        background: var(--color-accent);
        color: var(--color-ink);
    }

    /* An answer of 100 characters stays within its box: two lines at most. */
    .text {
        display: -webkit-box;
        overflow: hidden;
        font-weight: 800;
        overflow-wrap: anywhere;
        -webkit-box-orient: vertical;
        -webkit-line-clamp: 2;
        line-clamp: 2;
    }

    .reveal li svg {
        vertical-align: -0.125em;
        fill: none;
        stroke: currentColor;
        stroke-width: 3;
        stroke-linecap: round;
        stroke-linejoin: round;
    }

    .authors {
        color: var(--color-on-surface-muted);
        font-size: 1.25rem;
        overflow-wrap: anywhere;
    }

    .unanswered {
        font-size: 1.5rem;
        overflow-wrap: anywhere;
    }

    /* A fade alone: nothing moves, so that nothing ever crosses the edges of the screen. */
    @media (prefers-reduced-motion: no-preference) {
        h1,
        img {
            animation: appear 0.4s ease-out;
        }

        /* The expected answer and the answers accepted, the moment they are revealed. */
        .expected,
        .reveal li.correct {
            animation: reveal-highlight 0.6s ease-out;
        }

        /* The sticker of a player, the moment they answer. */
        .participants li.answered {
            animation: reveal-highlight 0.6s ease-out;
        }
    }

    @keyframes appear {
        from {
            opacity: 0;
        }
    }
</style>
