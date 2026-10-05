import { describe, expect, it } from 'vitest';
import { diagnosticText, diagnosticUrl, qualityParts } from './networkText';

describe('diagnosticText', () => {
    it('tells the device, the time, the verdict and the median round trip', () => {
        const reportedAt = new Date(2026, 9, 4, 18, 5, 9).getTime();

        expect(
            diagnosticText({ device: 'IPhone', reportedAt, verdict: 'Good', roundTripMedian: 12 }),
        ).toBe('iPhone à 18:05:09 : tout est bon · 12 ms');
        expect(
            diagnosticText({
                device: 'Other',
                reportedAt,
                verdict: 'Problem',
                roundTripMedian: null,
            }),
        ).toBe('Appareil à 18:05:09 : problème');
    });
});

describe('qualityParts', () => {
    it('lists the round trip, the clock uncertainty, the transport and the reconnections, marking the poor ones', () => {
        expect(
            qualityParts({
                playerId: null,
                transport: 'LongPolling',
                roundTrip: 240,
                reconnections: 1,
            }),
        ).toEqual([
            { text: '240 ms', poor: true },
            { text: 'horloge ±120 ms', poor: true },
            { text: 'connexion de repli (polling)', poor: true },
            { text: '1 reconnexion', poor: false },
        ]);
    });

    it('shows a round trip not measured yet without marking it, nor the clock', () => {
        expect(
            qualityParts({
                playerId: null,
                transport: 'WebSockets',
                roundTrip: null,
                reconnections: 0,
            })[0],
        ).toEqual({ text: '— ms', poor: false });
        expect(
            qualityParts({
                playerId: null,
                transport: 'WebSockets',
                roundTrip: null,
                reconnections: 0,
            })[1],
        ).toEqual({ text: 'horloge ± — ms', poor: false });
    });
});

describe('diagnosticUrl', () => {
    it('opens the diagnostic page next to the player page', () => {
        expect(diagnosticUrl('http://192.168.1.42:5000/')).toBe(
            'http://192.168.1.42:5000/diagnostic/',
        );
    });
});
