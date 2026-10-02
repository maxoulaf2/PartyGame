/** Longest nickname, in visible characters: `NicknameRules.MaxLength` on the server. */
export const nicknameMaxLength = 16;

/**
 * What is obviously wrong with a nickname, flagged before sending. The server applies the full
 * rules and alone decides: a nickname that passes here may still be refused.
 */
export type NicknameProblem = 'empty' | 'tooLong' | 'invalidCharacters';

// Tabs, line breaks and other control characters, which the server always refuses.
const controlCharacters = /[\p{Cc}\u2028\u2029]/u;
const spaces = /\s+/gu;
const graphemes = new Intl.Segmenter('fr', { granularity: 'grapheme' });

/** Tidies a nickname like the server: spaces trimmed and collapsed, composed Unicode form. */
export function tidyNickname(text: string): string {
    return text.trim().replace(spaces, ' ').normalize('NFC');
}

/** The obvious problem of `text`, or null when only the server can tell. */
export function checkNickname(text: string): NicknameProblem | null {
    if (controlCharacters.test(text)) {
        return 'invalidCharacters';
    }
    const nickname = tidyNickname(text);
    if (nickname === '') {
        return 'empty';
    }
    // Counted like the server, in what a reader sees as characters: an emoji counts as one.
    return Array.from(graphemes.segment(nickname)).length > nicknameMaxLength ? 'tooLong' : null;
}
