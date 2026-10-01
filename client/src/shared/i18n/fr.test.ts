import { describe, expect, it } from 'vitest';
import { fr } from './fr';

function collectTexts(node: unknown, path: string): [string, unknown][] {
    if (typeof node === 'object' && node !== null) {
        return Object.entries(node).flatMap(([key, value]) =>
            collectTexts(value, path ? `${path}.${key}` : key),
        );
    }
    return [[path, node]];
}

describe('fr', () => {
    it('contains only non-empty strings', () => {
        for (const [path, text] of collectTexts(fr, '')) {
            expect(typeof text, path).toBe('string');
            expect(String(text).trim(), path).not.toBe('');
        }
    });

    it('gives each role a distinct waiting text', () => {
        const waitingTexts = new Set([fr.player.waiting, fr.display.waiting, fr.gm.waiting]);
        expect(waitingTexts.size).toBe(3);
    });
});
