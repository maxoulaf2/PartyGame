import { describe, expect, it } from 'vitest';
import {
    isOnPodium,
    isRevealed,
    podiumPosition,
    splitFinalRanking,
    untilNextReveal,
} from './podium';

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

describe('podium reveal', () => {
    const finishedAt = 1_000_000;

    function revealedAt(elapsed: number) {
        return [5, 3, 2, 1].filter((rank) => isRevealed(rank, finishedAt, finishedAt + elapsed));
    }

    it('reveals the rest at once, then the third, second and first steps', () => {
        expect(revealedAt(0)).toEqual([5]);
        expect(revealedAt(2499)).toEqual([5]);
        expect(revealedAt(2500)).toEqual([5, 3]);
        expect(revealedAt(5000)).toEqual([5, 3, 2]);
        expect(revealedAt(7500)).toEqual([5, 3, 2, 1]);
    });

    it('waits for each step in turn, and stops once the first is revealed, within 10 s', () => {
        let elapsed = 0;
        const waits: number[] = [];
        for (let wait = untilNextReveal(finishedAt, finishedAt); wait !== null;) {
            waits.push(wait);
            elapsed += wait;
            wait = untilNextReveal(finishedAt, finishedAt + elapsed);
        }

        expect(waits).toEqual([2500, 2500, 2500]);
        expect(elapsed).toBeLessThan(10_000);
        expect(untilNextReveal(finishedAt, finishedAt + 3000)).toBe(2000);
    });

    it('shows everything at once to a screen opened after the reveal, or without an end time', () => {
        expect(untilNextReveal(finishedAt, finishedAt + 60_000)).toBeNull();
        expect(revealedAt(60_000)).toEqual([5, 3, 2, 1]);
        expect(untilNextReveal(null, finishedAt)).toBeNull();
        expect(isRevealed(1, null, finishedAt)).toBe(true);
    });
});
