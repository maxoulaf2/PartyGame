import { describe, expect, it } from 'vitest';
import { flashDuration, flashLit } from './flash';

describe('flashLit', () => {
    it('lights the screen at the start of each whole second of the server only', () => {
        const second = 1_791_000_000_000;

        expect(flashLit(second)).toBe(true);
        expect(flashLit(second + flashDuration - 1)).toBe(true);
        expect(flashLit(second + flashDuration)).toBe(false);
        expect(flashLit(second + 999)).toBe(false);
        expect(flashLit(second + 1_000.5)).toBe(true);
    });
});
