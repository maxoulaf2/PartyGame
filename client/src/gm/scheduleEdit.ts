import type { GameMasterScheduledRound, ScheduledRound } from '../shared/contracts';

/**
 * The rounds of the programme that may still change, as a request names them: those to come in
 * their order, then those withdrawn. The others are played or current, and never move.
 */
export function editableOrder(schedule: readonly GameMasterScheduledRound[]): ScheduledRound[] {
    return [
        ...schedule.filter((r) => r.status === 'Upcoming'),
        ...schedule.filter((r) => r.status === 'Withdrawn'),
    ].map((r) => ({ roundIndex: r.roundIndex, isWithdrawn: r.status === 'Withdrawn' }));
}

/**
 * The order once the round to come `roundIndex` swapped places with its neighbour, before it
 * (`-1`) or after it (`1`) among those to come; null when it has none on that side.
 */
export function moved(
    order: readonly ScheduledRound[],
    roundIndex: number,
    direction: -1 | 1,
): ScheduledRound[] | null {
    const at = order.findIndex((r) => r.roundIndex === roundIndex);
    const self = order[at];
    const other = order[at + direction];
    if (!self || !other || self.isWithdrawn || other.isWithdrawn) {
        return null;
    }
    const next = [...order];
    next[at] = other;
    next[at + direction] = self;
    return next;
}

/** The order once `roundIndex` is withdrawn: it joins the end of the rounds withdrawn. */
export function withdrawn(order: readonly ScheduledRound[], roundIndex: number): ScheduledRound[] {
    return [...order.filter((r) => r.roundIndex !== roundIndex), { roundIndex, isWithdrawn: true }];
}

/** The order once `roundIndex` is put back: last of the rounds to come. */
export function putBack(order: readonly ScheduledRound[], roundIndex: number): ScheduledRound[] {
    const others = order.filter((r) => r.roundIndex !== roundIndex);
    return [
        ...others.filter((r) => !r.isWithdrawn),
        { roundIndex, isWithdrawn: false },
        ...others.filter((r) => r.isWithdrawn),
    ];
}
