import { describe, expect, it } from 'vitest';
import { adjustedScore } from './scoreEntry';

describe('adjustedScore', () => {
    it('adds, removes or replaces the points', () => {
        expect(adjustedScore(1000, 'add', '500')).toBe(1500);
        expect(adjustedScore(1000, 'remove', ' 200 ')).toBe(800);
        expect(adjustedScore(1000, 'set', '0')).toBe(0);
    });

    it('lets a removal go below zero, for the form to refuse it', () => {
        expect(adjustedScore(100, 'remove', '200')).toBe(-100);
    });

    it('is null for anything but a whole number', () => {
        for (const amount of ['', '-5', '+5', '1.5', '12a', '99999999']) {
            expect(adjustedScore(100, 'add', amount)).toBeNull();
        }
    });
});
