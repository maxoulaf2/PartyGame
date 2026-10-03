import { describe, expect, it, vi } from 'vitest';
import type { ClientErrorReport } from '../contracts';
import type { GameConnection } from '../connection/gameHub';
import { reportFieldLimits } from './errorReport';
import { ErrorReporter, reportLimits, type ShownSnapshot } from './errorReporter';

/** A connection that records what is sent, and lets the test establish and lose it. */
function fakeConnection(invokeFails = false) {
    const connected: (() => void)[] = [];
    const reconnecting: (() => void)[] = [];
    const sent: ClientErrorReport[] = [];
    const connection = {
        onConnected: (callback: () => void) => connected.push(callback),
        onReconnecting: (callback: () => void) => reconnecting.push(callback),
        invoke: vi.fn((_method: string, report: ClientErrorReport) => {
            if (invokeFails) {
                return Promise.reject(new Error('disconnected'));
            }
            sent.push(report);
            return Promise.resolve(null);
        }),
    };
    return {
        // The fake only implements what the reporter uses, with loose signatures.
        typed: connection as unknown as GameConnection,
        invoke: connection.invoke,
        sent,
        establish: () => connected.forEach((callback) => callback()),
        lose: () => reconnecting.forEach((callback) => callback()),
    };
}

function createReporter() {
    let now = 1_000_000;
    const reporter = new ErrorReporter({
        role: 'Player',
        page: '/',
        buildId: 'b1',
        now: () => now,
    });
    return {
        reporter,
        advance: (ms: number) => {
            now += ms;
        },
    };
}

function errorWith(message: string, stack = `Error: ${message}\n    at view`): Error {
    const error = new Error(message);
    error.stack = stack;
    return error;
}

const shown: ShownSnapshot = { version: 7, roundViewType: 'quiz' };

/** Lets the rejected sends settle. */
const settle = () => new Promise((resolve) => setTimeout(resolve, 0));

describe('ErrorReporter', () => {
    it('sends a report with what the page showed', () => {
        const { reporter } = createReporter();
        const server = fakeConnection();
        reporter.connect(server.typed, () => shown);
        server.establish();

        reporter.report('RenderFailed', errorWith('boom'));

        expect(server.invoke).toHaveBeenCalledWith('ReportClientError', {
            role: 'Player',
            page: '/',
            kind: 'RenderFailed',
            message: 'Error: boom',
            stack: 'Error: boom\n    at view',
            roundViewType: 'quiz',
            snapshotVersion: 7,
            buildId: 'b1',
        } satisfies ClientErrorReport);
    });

    it('reports the same error once a minute', () => {
        const { reporter, advance } = createReporter();
        const server = fakeConnection();
        reporter.connect(server.typed, () => shown);
        server.establish();

        reporter.report('Error', errorWith('loop'));
        advance(reportLimits.windowMs - 1);
        reporter.report('Error', errorWith('loop'));
        advance(1);
        reporter.report('Error', errorWith('loop'));

        expect(server.sent).toHaveLength(2);
    });

    it('takes errors of another stack trace for other errors', () => {
        const { reporter } = createReporter();
        const server = fakeConnection();
        reporter.connect(server.typed, () => shown);
        server.establish();

        reporter.report('Error', errorWith('boom', 'at quiz'));
        reporter.report('Error', errorWith('boom', 'at ranking'));

        expect(server.sent).toHaveLength(2);
    });

    it('sends 10 reports a minute at most, all errors together', () => {
        const { reporter, advance } = createReporter();
        const server = fakeConnection();
        reporter.connect(server.typed, () => shown);
        server.establish();

        for (let i = 0; i < 15; i++) {
            reporter.report('Error', errorWith(`error ${i}`));
        }
        const withinTheMinute = server.sent.length;
        advance(reportLimits.windowMs);
        reporter.report('Error', errorWith('later'));

        expect(withinTheMinute).toBe(reportLimits.reportsPerWindow);
        expect(server.sent.at(-1)?.message).toBe('Error: later');
    });

    it('ignores noise such as the ResizeObserver loop warnings', () => {
        const { reporter } = createReporter();
        const server = fakeConnection();
        reporter.connect(server.typed, () => shown);
        server.establish();

        reporter.report('Error', 'ResizeObserver loop limit exceeded');

        expect(server.sent).toHaveLength(0);
    });

    it('keeps the reports made before the connection, and sends them once established', () => {
        const { reporter } = createReporter();
        const server = fakeConnection();
        reporter.report('Error', errorWith('early'));
        reporter.connect(server.typed, () => shown);

        server.establish();

        expect(server.sent.map((report) => report.message)).toEqual(['Error: early']);
        expect(reporter.queued).toHaveLength(0);
    });

    it('keeps the reports made while disconnected, and sends them after the reconnection', () => {
        const { reporter } = createReporter();
        const server = fakeConnection();
        reporter.connect(server.typed, () => shown);
        server.establish();
        server.lose();

        reporter.report('UnhandledRejection', 'offline');
        const sentWhileLost = server.sent.length;
        server.establish();

        expect(sentWhileLost).toBe(0);
        expect(server.sent.map((report) => report.kind)).toEqual(['UnhandledRejection']);
    });

    it('keeps 20 reports at most while disconnected, the first ones', () => {
        const { reporter, advance } = createReporter();
        for (let i = 0; i < 30; i++) {
            reporter.report('Error', errorWith(`error ${i}`));
            // A minute between two, as during a long outage: the rate limit lets each one through.
            advance(reportLimits.windowMs);
        }

        expect(reporter.queued).toHaveLength(reportLimits.queued);
        expect(reporter.queued[0]?.message).toBe('Error: error 0');
    });

    it('tells what the page showed when the error happened, not when the report leaves', () => {
        const { reporter } = createReporter();
        const server = fakeConnection();
        let current: ShownSnapshot = { version: 3, roundViewType: null };
        reporter.connect(server.typed, () => current);

        reporter.report('Error', errorWith('early'));
        current = shown;
        server.establish();

        expect(server.sent[0]?.snapshotVersion).toBe(3);
    });

    it('cuts the long fields', () => {
        let now = 0;
        const reporter = new ErrorReporter({
            role: 'Display',
            page: `/${'p'.repeat(1000)}`,
            buildId: 'b'.repeat(1000),
            now: () => now++,
        });
        const server = fakeConnection();
        reporter.connect(server.typed, () => ({ version: 1, roundViewType: 't'.repeat(1000) }));
        server.establish();

        reporter.report('Error', errorWith('m'.repeat(5000), 's'.repeat(10_000)));

        const report = server.sent[0];
        expect([
            report?.page.length,
            report?.message.length,
            report?.stack?.length,
            report?.roundViewType?.length,
            report?.buildId?.length,
        ]).toEqual([
            reportFieldLimits.page,
            reportFieldLimits.message,
            reportFieldLimits.stack,
            reportFieldLimits.roundViewType,
            reportFieldLimits.buildId,
        ]);
    });

    it('never throws nor reports again when sending fails', async () => {
        const { reporter } = createReporter();
        const server = fakeConnection(true);
        reporter.connect(server.typed, () => shown);
        server.establish();

        expect(() => reporter.report('Error', errorWith('boom'))).not.toThrow();
        await settle();

        // Kept to be sent again once the connection comes back, and nothing more.
        expect(server.invoke).toHaveBeenCalledTimes(1);
        expect(reporter.queued.map((report) => report.message)).toEqual(['Error: boom']);
    });

    it('never throws when what the page showed cannot be read', () => {
        const { reporter } = createReporter();
        const server = fakeConnection();
        reporter.connect(server.typed, () => {
            throw new Error('broken store');
        });
        server.establish();

        expect(() => reporter.report('Error', errorWith('boom'))).not.toThrow();
    });

    it('keeps the reports again once disconnected from the page', () => {
        const { reporter } = createReporter();
        const server = fakeConnection();
        const stop = reporter.connect(server.typed, () => shown);
        server.establish();

        stop();
        reporter.report('Error', errorWith('after'));

        expect(server.sent).toHaveLength(0);
        expect(reporter.queued[0]?.snapshotVersion).toBeNull();
    });
});
