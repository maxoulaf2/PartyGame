/** How the game master corrects a score: points added, points removed, or a new total. */
export type ScoreEntryMode = 'add' | 'remove' | 'set';

// Digits only: the sign is the mode, so that the numeric keyboard of a phone suffices.
const amountPattern = /^[0-9]{1,7}$/;

/**
 * The total `score` becomes once corrected by `amount` as typed, or null while `amount` is no
 * whole number. The result may be negative: the form refuses it, as the server does.
 */
export function adjustedScore(score: number, mode: ScoreEntryMode, amount: string): number | null {
    const trimmed = amount.trim();
    if (!amountPattern.test(trimmed)) {
        return null;
    }
    const points = Number(trimmed);
    return mode === 'add' ? score + points : mode === 'remove' ? score - points : points;
}
