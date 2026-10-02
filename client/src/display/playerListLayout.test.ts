import { describe, expect, it } from 'vitest';
import { playerListLayout } from './playerListLayout';

describe('playerListLayout', () => {
    it('gives a few players one column of large nicknames', () => {
        expect(playerListLayout(0).columns).toBe(1);
        expect(playerListLayout(6).columns).toBe(1);
    });

    it('splits the list into more columns and smaller type as players arrive', () => {
        const sizes = [1, 7, 13, 21].map((count) => playerListLayout(count));

        expect(sizes.map((layout) => layout.columns)).toEqual([1, 2, 2, 3]);
        const fontSizes = sizes.map((layout) => parseFloat(layout.fontSize));
        expect(fontSizes).toEqual([...fontSizes].sort((a, b) => b - a));
        expect(new Set(fontSizes).size).toBe(4);
    });
});
