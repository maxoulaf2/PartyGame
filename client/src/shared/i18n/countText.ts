/** The forms of a text that depends on a count, `{count}` standing for the number. */
export interface CountTexts {
    readonly zero: string;
    readonly one: string;
    readonly other: string;
}

/**
 * Picks the form of `texts` matching `count` and fills in the number. French puts 0 with 1 in the
 * same plural category, so zero gets its own form, often a different sentence altogether.
 */
export function countText(texts: CountTexts, count: number): string {
    const form = count === 0 ? texts.zero : count === 1 ? texts.one : texts.other;
    return form.replace('{count}', String(count));
}
