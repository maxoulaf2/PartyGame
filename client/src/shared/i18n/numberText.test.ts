import { describe, expect, it } from 'vitest';
import { formatNumber, formatSeconds } from './numberText';

describe('formatNumber', () => {
    it('groups the digits by three with a narrow no-break space', () => {
        expect(formatNumber(0)).toBe('0');
        expect(formatNumber(950)).toBe('950');
        expect(formatNumber(1350)).toBe('1 350');
        expect(formatNumber(1234567)).toBe('1 234 567');
    });
});

describe('formatSeconds', () => {
    it('writes the seconds to the tenth with a decimal comma', () => {
        expect(formatSeconds(0)).toBe('0,0');
        expect(formatSeconds(4250)).toBe('4,3');
        expect(formatSeconds(12_340)).toBe('12,3');
    });
});
