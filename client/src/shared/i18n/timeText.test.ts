import { describe, expect, it } from 'vitest';
import { formatTime } from './timeText';

describe('formatTime', () => {
    it('writes hours, minutes and seconds on two digits, on the clock of the device', () => {
        const instant = new Date(2026, 9, 3, 21, 4, 5).getTime();

        expect(formatTime(instant)).toBe('21:04:05');
    });
});
