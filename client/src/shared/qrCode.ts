import { encode } from 'uqr';

/** The quiet zone the QR specification requires around the code, in modules. */
export const quietZone = 4;

export interface QrCodeShape {
    /** Width and height in modules, quiet zone included. */
    readonly size: number;
    /** SVG path covering every dark module. */
    readonly path: string;
}

/**
 * Encodes text as a QR code drawn by a single SVG path. Medium error correction: join URLs are
 * short, and a less dense code stays readable from across the room.
 */
export function qrCodeShape(text: string): QrCodeShape {
    const { size, data } = encode(text, { ecc: 'M', border: quietZone });
    const path = data
        .flatMap((row, y) =>
            darkRuns(row).map(([x, length]) => `M${x} ${y}h${length}v1h-${length}z`),
        )
        .join('');
    return { size, path };
}

/** Start and length of each horizontal run of dark modules, so the path stays compact. */
function darkRuns(row: readonly boolean[]): [number, number][] {
    const runs: [number, number][] = [];
    let start = -1;
    for (let x = 0; x <= row.length; x++) {
        const dark = row[x] === true;
        if (dark && start < 0) {
            start = x;
        } else if (!dark && start >= 0) {
            runs.push([start, x - start]);
            start = -1;
        }
    }
    return runs;
}
