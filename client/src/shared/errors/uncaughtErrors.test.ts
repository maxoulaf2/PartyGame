import { describe, expect, it, vi } from 'vitest';
import { catchUncaughtErrors } from './uncaughtErrors';

/** An event of `window`, with the fields the browser gives it. */
function eventOf(type: string, fields: Record<string, unknown>): Event {
    return Object.assign(new Event(type), fields);
}

describe('catchUncaughtErrors', () => {
    it('hands over an uncaught error with the script the browser blames', () => {
        const target = new EventTarget();
        const sink = vi.fn();
        catchUncaughtErrors(sink, target);
        const error = new TypeError('x is null');

        target.dispatchEvent(
            eventOf('error', { error, message: 'x is null', filename: 'http://host/a.js' }),
        );

        expect(sink).toHaveBeenCalledWith('Error', error, 'http://host/a.js');
    });

    it('hands over the message alone when the browser gives no error', () => {
        const target = new EventTarget();
        const sink = vi.fn();
        catchUncaughtErrors(sink, target);

        target.dispatchEvent(
            eventOf('error', { error: null, message: 'Script error.', filename: '' }),
        );

        expect(sink).toHaveBeenCalledWith('Error', 'Script error.', null);
    });

    it('hands over the reason of a promise rejected without handler', () => {
        const target = new EventTarget();
        const sink = vi.fn();
        catchUncaughtErrors(sink, target);

        target.dispatchEvent(eventOf('unhandledrejection', { reason: 'timeout' }));

        expect(sink).toHaveBeenCalledWith('UnhandledRejection', 'timeout', null);
    });

    it('stops listening once stopped', () => {
        const target = new EventTarget();
        const sink = vi.fn();
        const stop = catchUncaughtErrors(sink, target);

        stop();
        target.dispatchEvent(eventOf('error', { error: new Error('late') }));
        target.dispatchEvent(eventOf('unhandledrejection', { reason: 'late' }));

        expect(sink).not.toHaveBeenCalled();
    });
});
