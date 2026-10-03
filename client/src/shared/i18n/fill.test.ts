import { describe, expect, it } from 'vitest';
import { fill } from './fill';

describe('fill', () => {
    it('fills every placeholder with its value, numbers included', () => {
        expect(
            fill('Manche {number}/{count} : {title}', { number: 1, count: 2, title: 'Quiz' }),
        ).toBe('Manche 1/2 : Quiz');
    });

    it('keeps a placeholder without a value', () => {
        expect(fill('{title} ({mode})', { title: 'Quiz' })).toBe('Quiz ({mode})');
    });

    it('shows a value as written, even when it looks like a replacement pattern', () => {
        expect(fill('Pack : {title}', { title: '$& $1 $$' })).toBe('Pack : $& $1 $$');
    });
});
