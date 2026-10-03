import type { QuizChoiceLetter } from '../../shared/contracts';

/** The color of the choice `letter`, a variable of the theme. */
export function choiceColor(letter: QuizChoiceLetter): string {
    return `var(--choice-${letter.toLowerCase()}-color)`;
}

/** The shape of the choice `letter`, a clip-path variable of the theme. */
export function choiceShape(letter: QuizChoiceLetter): string {
    return `var(--choice-${letter.toLowerCase()}-shape)`;
}
