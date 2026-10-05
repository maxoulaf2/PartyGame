import { describe, expect, it } from 'vitest';
import type { Incident, RoundId } from '../shared/contracts';
import { describeIncident, incidentRoundText, incidentTimeText } from './incidentText';

const lastOccurredAt = new Date(2026, 9, 3, 21, 4, 5).getTime();

function incident(overrides: Partial<Incident> = {}): Incident {
    return {
        id: 1,
        code: 'RoundHandlerFailed',
        round: null,
        role: null,
        step: null,
        count: 1,
        lastOccurredAt,
        ...overrides,
    };
}

describe('describeIncident', () => {
    it('says what went wrong, without any technical detail', () => {
        expect(describeIncident(incident())).toBe(
            'Une action n’a pas pu être traitée : elle a été ignorée.',
        );
        expect(describeIncident(incident({ code: 'EffectFailed' }))).toBe(
            'Une opération a échoué après une action : la partie continue.',
        );
        expect(describeIncident(incident({ code: 'DisplayMediaFailed', step: 2 }))).toBe(
            'L’écran TV n’a pas pu charger une image ou un son : le public n’en voit rien.',
        );
    });

    it('names the screens that kept their previous state', () => {
        expect(describeIncident(incident({ code: 'ProjectionFailed', role: 'Display' }))).toBe(
            'L’affichage de l’écran TV n’a pas pu être mis à jour : il garde l’état précédent.',
        );
        expect(describeIncident(incident({ code: 'ProjectionFailed', role: 'Player' }))).toBe(
            'L’affichage des téléphones n’a pas pu être mis à jour : il garde l’état précédent.',
        );
    });
});

describe('incidentRoundText', () => {
    it('names the round in progress, or says there was none', () => {
        const round = {
            roundId: '0f8fad5b-d9cb-469f-a165-70867728950e' as RoundId,
            number: 2,
            count: 3,
            title: 'Cinéma',
        };

        expect(incidentRoundText(incident({ round }))).toBe('Manche 2/3 : Cinéma');
        expect(incidentRoundText(incident())).toBe('Hors manche');
    });

    it('names the question of the round concerned, when the server tells it', () => {
        const round = {
            roundId: '0f8fad5b-d9cb-469f-a165-70867728950e' as RoundId,
            number: 1,
            count: 2,
            title: 'Drapeaux',
        };

        expect(incidentRoundText(incident({ code: 'DisplayMediaFailed', round, step: 4 }))).toBe(
            'Manche 1/2 : Drapeaux, question 4',
        );
    });
});

describe('incidentTimeText', () => {
    it('gives the time of a single occurrence', () => {
        expect(incidentTimeText(incident())).toBe('À 21:04:05');
    });

    it('gives the count of a repeated incident and the time of its last occurrence', () => {
        expect(incidentTimeText(incident({ count: 3 }))).toBe('3 fois, la dernière à 21:04:05');
    });
});
