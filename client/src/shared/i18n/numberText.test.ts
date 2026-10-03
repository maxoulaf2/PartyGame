import { describe, expect, it } from 'vitest';
import { formatNumber } from './numberText';

describe('formatNumber', () => {
    it('groups the digits by three with a narrow no-break space', () => {
        expect(formatNumber(0)).toBe('0');
        expect(formatNumber(950)).toBe('950');
        expect(formatNumber(1350)).toBe('1 350');
        expect(formatNumber(1234567)).toBe('1 234 567');
    });
});
