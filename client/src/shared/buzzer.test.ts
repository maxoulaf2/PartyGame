import { describe, expect, it, vi } from 'vitest';
import { BuzzerPresses } from './buzzer.svelte';

/** The page loaded at 20:00:00 UTC, by its own clock, and the server runs 2.5 s ahead. */
const origin = Date.UTC(2026, 9, 5, 20, 0, 0);
const serverAhead = 2_500;

function buzzer(synchronized = true) {
    const clock = {
        synchronized,
        toServerTime: (timestamp: number) => origin + timestamp + serverAhead,
    };
    const buzz = vi.fn<(pressedAt: number) => void>();
    return { presses: new BuzzerPresses(clock, buzz), buzz };
}

describe('BuzzerPresses', () => {
    it('buzzes with the time of the press on the clock of the server', () => {
        const { presses, buzz } = buzzer();

        expect(presses.press('open', '1:1', true, 1_234.5)).toBe(true);

        expect(buzz).toHaveBeenCalledExactlyOnceWith(origin + 1_234.5 + serverAhead);
    });

    it('buzzes once per opening, however many presses', () => {
        const { presses, buzz } = buzzer();

        presses.press('open', '1:1', true, 100);
        expect(presses.press('open', '1:1', true, 101)).toBe(false);
        expect(presses.shown('open', '1:1')).toBe('sent');
        expect(presses.press('open', '1:2', true, 500)).toBe(true);

        expect(buzz.mock.calls).toEqual([
            [origin + 100 + serverAhead],
            [origin + 500 + serverAhead],
        ]);
    });

    it.each(['closed', 'sent', 'won', 'lost', 'blocked'] as const)(
        'does not buzz while %s',
        (state) => {
            const { presses, buzz } = buzzer();

            expect(presses.enabled(state, '1:1', true)).toBe(false);
            expect(presses.press(state, '1:1', true, 100)).toBe(false);
            expect(buzz).not.toHaveBeenCalled();
        },
    );

    it('does not buzz without a fresh snapshot', () => {
        const { presses, buzz } = buzzer();

        expect(presses.press('open', '1:1', false, 100)).toBe(false);
        expect(buzz).not.toHaveBeenCalled();
    });

    it('does not buzz before the clock is synchronized', () => {
        const { presses, buzz } = buzzer(false);

        expect(presses.enabled('open', '1:1', true)).toBe(false);
        expect(presses.press('open', '1:1', true, 100)).toBe(false);
        expect(buzz).not.toHaveBeenCalled();
    });

    it('lets the snapshot win once it tells what became of the buzz', () => {
        const { presses } = buzzer();

        presses.press('open', '1:1', true, 100);

        expect(presses.shown('won', '1:1')).toBe('won');
        expect(presses.shown('closed', '1:1')).toBe('closed');
    });
});
