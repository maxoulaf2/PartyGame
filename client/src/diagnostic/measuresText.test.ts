import { describe, expect, it } from 'vitest';
import { describeClock, describeMeasures } from './measuresText';

describe('describeMeasures', () => {
    it('lists every measure in French', () => {
        const lines = describeMeasures({
            transport: 'WebSockets',
            sameSubnet: false,
            roundTrips: { median: 12.4, max: 30, jitter: 3.6 },
            pings: 15,
            lost: 1,
            reconnections: 0,
            throughput: 8.25,
        });

        expect(lines.map((line) => `${line.label} : ${line.value}`)).toEqual([
            'Connexion : WebSocket',
            'Temps de réponse : médiane 12 ms · max 30 ms · gigue 4 ms',
            'Stabilité : 1 perte sur 15 · aucune coupure',
            'Débit : 8,3 Mbit/s',
            'Réseau : Autre réseau que le serveur',
        ]);
    });

    it('says only that the server is out of reach when it is', () => {
        expect(
            describeMeasures({
                transport: null,
                sameSubnet: null,
                roundTrips: null,
                pings: 0,
                lost: 0,
                reconnections: 0,
                throughput: null,
            }),
        ).toEqual([{ label: 'Connexion', value: 'Serveur injoignable' }]);
    });
});

describe('describeClock', () => {
    it('tells the round trip and the uncertainty it leaves, half of it', () => {
        expect(describeClock(24)).toBe('aller-retour 24 ms · incertitude ±12 ms');
    });
});
