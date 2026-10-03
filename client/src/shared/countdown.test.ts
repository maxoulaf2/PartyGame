import { describe, expect, it } from 'vitest';
import { secondsLeft, untilNextSecond } from './countdown';

const closeAt = 1_790_000_020_000;

describe('secondsLeft', () => {
    it.each([
        [20_000, 20],
        [19_999, 20],
        [19_001, 20],
        [19_000, 19],
        [1, 1],
        [0, 0],
        [-500, 0],
    ])('shows %i ms before the close as %i', (left, shown) => {
        expect(secondsLeft(closeAt, closeAt - left)).toBe(shown);
    });
});

describe('untilNextSecond', () => {
    it.each([
        [20_000, 1000],
        [19_999, 999],
        [19_001, 1],
        [1, 1],
    ])('waits, %i ms before the close, %i ms for the next number', (left, wait) => {
        expect(untilNextSecond(closeAt, closeAt - left)).toBe(wait);
    });

    it('waits for nothing once it shows 0', () => {
        expect(untilNextSecond(closeAt, closeAt)).toBeNull();
        expect(untilNextSecond(closeAt, closeAt + 1)).toBeNull();
    });

    it('changes the number exactly when a whole second passes', () => {
        const now = closeAt - 19_250;
        expect(secondsLeft(closeAt, now)).toBe(20);
        const next = now + (untilNextSecond(closeAt, now) ?? 0);
        expect(secondsLeft(closeAt, next)).toBe(19);
        expect(secondsLeft(closeAt, next - 1)).toBe(20);
    });
});
