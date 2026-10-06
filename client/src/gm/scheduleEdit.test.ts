import { describe, expect, it } from 'vitest';
import type { GameMasterScheduledRound, ScheduledRoundStatus } from '../shared/contracts';
import { editableOrder, moved, putBack, withdrawn } from './scheduleEdit';

const round = (roundIndex: number, status: ScheduledRoundStatus): GameMasterScheduledRound => ({
    roundIndex,
    title: `Manche ${roundIndex}`,
    mode: 'quiz',
    status,
});

const schedule = [
    round(0, 'Played'),
    round(1, 'Current'),
    round(3, 'Upcoming'),
    round(2, 'Upcoming'),
    round(4, 'Withdrawn'),
];

const indexes = (order: { roundIndex: number; isWithdrawn: boolean }[] | null) =>
    order?.map((r) => `${r.roundIndex}${r.isWithdrawn ? '-' : ''}`).join(' ');

describe('editableOrder', () => {
    it('names the rounds to come, then those withdrawn', () => {
        expect(indexes(editableOrder(schedule))).toBe('3 2 4-');
    });
});

describe('moved', () => {
    const order = editableOrder(schedule);

    it('swaps a round to come with its neighbour', () => {
        expect(indexes(moved(order, 2, -1))).toBe('2 3 4-');
        expect(indexes(moved(order, 3, 1))).toBe('2 3 4-');
    });

    it('never moves past the rounds to come', () => {
        expect(moved(order, 3, -1)).toBeNull();
        expect(moved(order, 2, 1)).toBeNull();
        expect(moved(order, 4, -1)).toBeNull();
        expect(moved(order, 1, 1)).toBeNull();
    });
});

describe('withdrawn and putBack', () => {
    const order = editableOrder(schedule);

    it('withdraws a round after the others withdrawn', () => {
        expect(indexes(withdrawn(order, 3))).toBe('2 4- 3-');
    });

    it('puts a round back last of those to come', () => {
        expect(indexes(putBack(order, 4))).toBe('3 2 4');
        expect(indexes(putBack(withdrawn(order, 3), 3))).toBe('2 3 4-');
    });
});
