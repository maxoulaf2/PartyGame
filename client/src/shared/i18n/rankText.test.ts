import { describe, expect, it } from 'vitest';
import { fr } from './fr';
import { rankText, standingText } from './rankText';

describe('rankText', () => {
    it('writes the first rank with its irregular ordinal', () => {
        expect(rankText(fr.game.rank, 1)).toBe('1er');
    });

    it('writes the other ranks with « e »', () => {
        expect(rankText(fr.game.rank, 2)).toBe('2e');
        expect(rankText(fr.game.rank, 11)).toBe('11e');
        expect(rankText(fr.game.rank, 21)).toBe('21e');
    });

    it('groups the digits of a large rank the French way', () => {
        expect(rankText(fr.game.rank, 1000)).toBe('1 000e');
    });
});

describe('standingText', () => {
    it('writes a rank held alone out of the number of players', () => {
        expect(standingText(fr.game.standing, fr.game.rank, { rank: 1, isTied: false }, 9)).toBe(
            '1er sur 9',
        );
    });

    it('writes a shared rank as ex aequo', () => {
        expect(standingText(fr.game.standing, fr.game.rank, { rank: 3, isTied: true }, 9)).toBe(
            '3e ex aequo sur 9',
        );
    });
});
