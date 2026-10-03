import { describe, expect, it } from 'vitest';
import { isOnPodium, podiumPosition, splitFinalRanking } from './podium';

function ranked(...ranks: number[]) {
    return ranks.map((rank, index) => ({ nickname: `Joueur ${index + 1}`, rank }));
}

describe('isOnPodium', () => {
    it('puts the first three ranks on the podium', () => {
        expect([1, 2, 3, 4, 10].map(isOnPodium)).toEqual([true, true, true, false, false]);
    });
});

describe('splitFinalRanking', () => {
    it('gives each of the first three ranks its step, and keeps the others in order', () => {
        const ranking = ranked(1, 2, 3, 4, 5);

        const { steps, rest } = splitFinalRanking(ranking);

        expect(steps.map((step) => [step.rank, step.players.map((p) => p.nickname)])).toEqual([
            [1, ['Joueur 1']],
            [2, ['Joueur 2']],
            [3, ['Joueur 3']],
        ]);
        expect(rest).toEqual([ranking[3], ranking[4]]);
    });

    it('puts ex aequo on the same step, in the order received, and skips the ranks they take', () => {
        const ranking = ranked(1, 1, 3, 3, 3, 6);

        const { steps, rest } = splitFinalRanking(ranking);

        expect(steps.map((step) => [step.rank, step.players.map((p) => p.nickname)])).toEqual([
            [1, ['Joueur 1', 'Joueur 2']],
            [3, ['Joueur 3', 'Joueur 4', 'Joueur 5']],
        ]);
        expect(rest).toEqual([ranking[5]]);
    });

    it('puts every player on the first step when all are ex aequo', () => {
        const { steps, rest } = splitFinalRanking(ranked(1, 1, 1, 1));

        expect(steps).toHaveLength(1);
        expect(steps[0]?.players).toHaveLength(4);
        expect(rest).toEqual([]);
    });

    it('has neither step nor rest without any player', () => {
        expect(splitFinalRanking([])).toEqual({ steps: [], rest: [] });
    });
});

describe('podiumPosition', () => {
    it('places the first step in the middle, the second on its left and the third on its right', () => {
        expect([2, 1, 3].map(podiumPosition)).toEqual([1, 2, 3]);
    });
});
