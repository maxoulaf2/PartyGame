import { describe, expect, it, vi } from 'vitest';
import { BoundaryRetry } from './boundaryRetry';

const first = { version: 1 };
const second = { version: 2 };
const third = { version: 3 };

describe('BoundaryRetry', () => {
    it('tries a failed view again once the page shows something else', () => {
        const retry = new BoundaryRetry();
        const reset = vi.fn();
        retry.failed(first, reset);

        retry.show(second);

        expect(reset).toHaveBeenCalledOnce();
    });

    it('waits for a change: the same snapshot would fail the same way', () => {
        const retry = new BoundaryRetry();
        const reset = vi.fn();
        retry.failed(first, reset);

        retry.show(first);

        expect(reset).not.toHaveBeenCalled();
    });

    it('tries once per change, never in a loop', () => {
        const retry = new BoundaryRetry();
        const firstReset = vi.fn();
        retry.failed(first, firstReset);
        retry.show(second);

        // Tried again, it fails with the second snapshot too: only a third one tries once more.
        const secondReset = vi.fn();
        retry.failed(second, secondReset);
        retry.show(second);
        retry.show(third);
        retry.show(first);

        expect(firstReset).toHaveBeenCalledOnce();
        expect(secondReset).toHaveBeenCalledOnce();
    });

    it('does nothing while the view renders', () => {
        const retry = new BoundaryRetry();

        expect(() => retry.show(second)).not.toThrow();
    });
});
