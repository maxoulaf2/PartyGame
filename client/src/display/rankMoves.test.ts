import { describe, expect, it } from 'vitest';
import { placesGained, previousIndexes } from './rankMoves';

describe('placesGained', () => {
    it('counts the places gained or lost, and none without a previous rank', () => {
        expect(placesGained({ rank: 1, previousRank: 3 })).toBe(2);
        expect(placesGained({ rank: 4, previousRank: 3 })).toBe(-1);
        expect(placesGained({ rank: 2, previousRank: 2 })).toBe(0);
        expect(placesGained({ rank: 2, previousRank: null })).toBeNull();
    });
});

describe('previousIndexes', () => {
    it('puts each row back where the previous ranking had it, newcomers last', () => {
        const ranking = [
            { rank: 1, previousRank: 2 },
            { rank: 2, previousRank: 1 },
            { rank: 3, previousRank: null },
            { rank: 4, previousRank: 3 },
        ];

        expect(previousIndexes(ranking)).toEqual([1, 0, 3, 2]);
    });

    it('keeps ex aequo in the order received', () => {
        const ranking = [
            { rank: 1, previousRank: 2 },
            { rank: 2, previousRank: 2 },
            { rank: 3, previousRank: 1 },
        ];

        expect(previousIndexes(ranking)).toEqual([1, 2, 0]);
    });
});
