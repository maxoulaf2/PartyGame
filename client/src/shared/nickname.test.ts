import { describe, expect, it } from 'vitest';
import { checkNickname, tidyNickname } from './nickname';

describe('tidyNickname', () => {
    it('trims and collapses spaces like the server', () => {
        expect(tidyNickname('  Jean   Paul ')).toBe('Jean Paul');
    });

    it('composes accents', () => {
        expect(tidyNickname('Zoe\u0301')).toBe('Zoé');
    });
});

describe('checkNickname', () => {
    it.each([
        'Zoé',
        '  Jean   Paul ',
        '16 characters ok',
        '🎉🎉🎉🎉🎉🎉🎉🎉🎉🎉🎉🎉🎉🎉🎉🎉',
        '👨‍👩‍👧 family',
    ])('leaves %j to the server', (nickname) => {
        expect(checkNickname(nickname)).toBeNull();
    });

    it.each(['', '   '])('flags %j as empty', (nickname) => {
        expect(checkNickname(nickname)).toBe('empty');
    });

    it.each(['Seventeen chars!!', '🎉🎉🎉🎉🎉🎉🎉🎉🎉🎉🎉🎉🎉🎉🎉🎉🎉'])(
        'flags %j as too long',
        (nickname) => {
            expect(checkNickname(nickname)).toBe('tooLong');
        },
    );

    it.each(['Zo\té', 'Zoé\n', 'Zo\u0007é', 'Zo\u2028é'])(
        'flags %j for its control characters',
        (nickname) => {
            expect(checkNickname(nickname)).toBe('invalidCharacters');
        },
    );
});
