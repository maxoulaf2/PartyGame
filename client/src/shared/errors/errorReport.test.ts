import { describe, expect, it } from 'vitest';
import { describeError, isIgnoredError, truncate } from './errorReport';

describe('truncate', () => {
    it('keeps a value within the limit as it is', () => {
        expect(truncate('abcd', 4)).toBe('abcd');
    });

    it('cuts a longer value to the limit', () => {
        expect(truncate('abcdef', 4)).toBe('abcd');
    });

    it('never leaves half of a surrogate pair', () => {
        expect(truncate('abc🎉d', 4)).toBe('abc');
    });
});

describe('describeError', () => {
    it('describes an error by its name, its message and its stack trace', () => {
        const error = new TypeError('x is null');
        error.stack = 'TypeError: x is null\n    at view';

        expect(describeError(error)).toEqual({
            message: 'TypeError: x is null',
            stack: 'TypeError: x is null\n    at view',
        });
    });

    it('describes an error without stack trace', () => {
        const error = new Error('boom');
        error.stack = '';

        expect(describeError(error)).toEqual({ message: 'Error: boom', stack: null });
    });

    it('describes a string as its message', () => {
        expect(describeError('rejected')).toEqual({ message: 'rejected', stack: null });
    });

    it('describes a plain object by its JSON', () => {
        expect(describeError({ code: 42 })).toEqual({ message: '{"code":42}', stack: null });
    });

    it('describes undefined', () => {
        expect(describeError(undefined)).toEqual({ message: 'undefined', stack: null });
    });

    it('describes an object it cannot serialize by its type', () => {
        const cyclic: { self?: unknown } = {};
        cyclic.self = cyclic;

        expect(describeError(cyclic)).toEqual({ message: 'Unreadable object', stack: null });
    });
});

describe('isIgnoredError', () => {
    const ours = { message: 'TypeError: x is null', stack: 'at view' };

    it('keeps an error of the pages', () => {
        expect(isIgnoredError(ours, 'http://192.168.1.10:5000/assets/player.js')).toBe(false);
    });

    it('ignores the ResizeObserver loop warnings', () => {
        const description = {
            message: 'ResizeObserver loop completed with undelivered notifications.',
            stack: null,
        };

        expect(isIgnoredError(description, null)).toBe(true);
    });

    it('ignores the opaque errors of scripts of another origin', () => {
        expect(isIgnoredError({ message: 'Script error.', stack: null }, null)).toBe(true);
    });

    it('keeps an error mentioning a script error when it has a stack trace', () => {
        expect(isIgnoredError({ message: 'Error: Script error.', stack: 'at view' }, null)).toBe(
            false,
        );
    });

    it.each([
        'chrome-extension://abc/content.js',
        'moz-extension://abc/content.js',
        'safari-web-extension://abc/content.js',
    ])('ignores the errors of a browser extension (%s)', (source) => {
        expect(isIgnoredError(ours, source)).toBe(true);
    });
});
