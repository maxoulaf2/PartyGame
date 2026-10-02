import { describe, expect, it } from 'vitest';
import packProblemCodeSource from '../contracts/PackProblemCode.ts?raw';
import { fr } from './fr';

/** Every pack problem code the server may report, read from the generated union type. */
function packProblemCodes(): string[] {
    const union = /export type PackProblemCode =([^;]+);/.exec(packProblemCodeSource)?.[1] ?? '';
    return [...union.matchAll(/'(\w+)'/g)].map((match) => match[1] ?? '');
}

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

    it('has a message for every pack problem code, and for no other', () => {
        const codes = packProblemCodes();

        expect(codes.length).toBeGreaterThan(10);
        expect(Object.keys(fr.gm.packs.problems).sort()).toEqual([...codes].sort());
    });
});
