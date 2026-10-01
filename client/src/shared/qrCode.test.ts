import { encode } from 'uqr';
import { describe, expect, it } from 'vitest';
import { qrCodeShape, quietZone } from './qrCode';

const url = 'http://192.168.1.42:5000/';

/** Redraws the path on a grid, to compare it with the matrix uqr encoded. */
function rasterize(path: string, size: number): boolean[][] {
    const grid = Array.from({ length: size }, () => Array<boolean>(size).fill(false));
    for (const [, x, y, length] of path.matchAll(/M(\d+) (\d+)h(\d+)v1h-\d+z/g)) {
        const row = grid[Number(y)];
        for (let i = 0; row && i < Number(length); i++) {
            row[Number(x) + i] = true;
        }
    }
    return grid;
}

describe('qrCodeShape', () => {
    it('draws exactly the dark modules of the medium correction code', () => {
        const expected = encode(url, { ecc: 'M', border: quietZone });

        const { size, path } = qrCodeShape(url);

        expect(size).toBe(expected.size);
        expect(rasterize(path, size)).toEqual(expected.data);
    });

    it('keeps a light quiet zone of four modules', () => {
        const { size, path } = qrCodeShape(url);
        const grid = rasterize(path, size);

        for (let i = 0; i < size; i++) {
            for (let depth = 0; depth < quietZone; depth++) {
                expect(grid[depth]?.[i]).toBe(false);
                expect(grid[size - 1 - depth]?.[i]).toBe(false);
                expect(grid[i]?.[depth]).toBe(false);
                expect(grid[i]?.[size - 1 - depth]).toBe(false);
            }
        }
        // The top-left finder pattern starts right after the quiet zone.
        expect(grid[quietZone]?.[quietZone]).toBe(true);
    });

    it('uses the smallest version that fits a join url', () => {
        // Version 2 (25 modules) holds up to 26 bytes at level M: a sparse code, easy to read from afar.
        expect(qrCodeShape(url).size).toBe(25 + 2 * quietZone);
    });
});
