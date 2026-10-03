import type { RoundInfo } from '../contracts';

/**
 * Fills the `{name}` placeholders of `text` with `values`. A placeholder without a value stays as
 * is. Function replacements: a value such as « $& » shows as written, never as a pattern.
 */
export function fill(text: string, values: Readonly<Record<string, string | number>>): string {
    return text.replace(/\{(\w+)\}/g, (placeholder, name: string) => {
        const value = values[name];
        return value === undefined ? placeholder : String(value);
    });
}

/** Fills the `{number}` and `{count}` placeholders of `text` with those of `round`. */
export function roundText(text: string, round: RoundInfo): string {
    return fill(text, { number: round.number, count: round.count });
}
