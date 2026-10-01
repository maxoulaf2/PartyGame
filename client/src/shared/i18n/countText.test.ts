import { describe, expect, it } from 'vitest';
import { countText } from './countText';

const texts = { zero: 'Personne', one: '{count} joueur', other: '{count} joueurs' };

describe('countText', () => {
    it('picks the form matching the count and fills in the number', () => {
        expect(countText(texts, 0)).toBe('Personne');
        expect(countText(texts, 1)).toBe('1 joueur');
        expect(countText(texts, 2)).toBe('2 joueurs');
        expect(countText(texts, 12)).toBe('12 joueurs');
    });
});
