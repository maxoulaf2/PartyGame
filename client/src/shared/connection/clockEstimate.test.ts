import { describe, expect, it } from 'vitest';
import { estimateClock, offsetOf, roundTripOf, type ClockSample } from './clockEstimate';

/** A server clock 1 000 ms ahead: the sample of a round trip whose request took `out` ms and answer `back` ms. */
function sample(sentAt: number, out: number, back: number, offset = 1_000): ClockSample {
    return { sentAt, serverTime: sentAt + out + offset, receivedAt: sentAt + out + back };
}

describe('offsetOf and roundTripOf', () => {
    it('measure a symmetric round trip exactly', () => {
        const s = sample(10_000, 20, 20);

        expect(roundTripOf(s)).toBe(40);
        expect(offsetOf(s)).toBe(1_000);
    });

    it('are wrong by half the asymmetry of the round trip', () => {
        expect(offsetOf(sample(10_000, 30, 10))).toBe(1_010);
    });
});

describe('estimateClock', () => {
    it('has no estimate without samples', () => {
        expect(estimateClock([])).toBeNull();
    });

    it('takes the only sample as it is', () => {
        expect(estimateClock([sample(0, 15, 25)])).toEqual({ offset: 995, roundTrip: 40 });
    });

    it('keeps the two fastest of eight round trips and ignores the slow outliers', () => {
        const samples = [
            sample(0, 400, 10), // delayed on the way out: offset off by 195 ms
            sample(100, 12, 12),
            sample(200, 10, 900), // delayed on the way back: offset off by -445 ms
            sample(300, 60, 20),
            sample(400, 11, 13),
            sample(500, 25, 35),
            sample(600, 300, 300),
            sample(700, 40, 18),
        ];

        expect(estimateClock(samples)).toEqual({ offset: 999.5, roundTrip: 24 });
    });

    it('takes the median offset of the fastest round trips', () => {
        // Twelve samples: the three fastest count, and the median sets the asymmetric one aside.
        const fast = [sample(0, 10, 10), sample(100, 2, 18), sample(200, 11, 11)];
        const slow = Array.from({ length: 9 }, (_, i) => sample(300 + i * 100, 50, 50 + i));

        expect(estimateClock([...slow, ...fast])).toEqual({ offset: 1_000, roundTrip: 20 });
    });

    it('does not depend on the order of the samples', () => {
        const samples = [
            sample(0, 30, 30),
            sample(100, 5, 7),
            sample(200, 50, 10),
            sample(300, 6, 6),
        ];

        expect(estimateClock([...samples].reverse())).toEqual(estimateClock(samples));
    });

    it('estimates a server clock behind the local one', () => {
        const samples = Array.from({ length: 8 }, (_, i) =>
            sample(i * 50, 8 + i, 8 + i, -3_600_000),
        );

        expect(estimateClock(samples)).toEqual({ offset: -3_600_000, roundTrip: 16 });
    });
});
