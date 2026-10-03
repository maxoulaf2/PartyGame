import type { ClientErrorKind, ClientErrorReport, Role } from '../contracts';
import type { GameConnection } from '../connection/gameHub';
import {
    describeError,
    isIgnoredError,
    reportFieldLimits,
    truncate,
    type ErrorDescription,
} from './errorReport';

/** How often a page reports its errors, at most. */
export const reportLimits = {
    /** The window over which the reports are counted, in milliseconds. */
    windowMs: 60_000,
    /** Reports per window, all errors together. */
    reportsPerWindow: 10,
    /** Reports kept while the page is disconnected, sent once it is back. */
    queued: 20,
} as const;

/** What the page showed when an error happened, so that the operator can replay it. */
export interface ShownSnapshot {
    /** The version of the snapshot shown, or null before the first. */
    readonly version: number | null;
    /** The `type` of the round view shown, or null outside a round. */
    readonly roundViewType: string | null;
}

const nothingShown: ShownSnapshot = { version: null, roundViewType: null };

/** What an `ErrorReporter` tells about its page in every report. */
export interface ErrorReporterOptions {
    /** The role of the page, which may not have announced it yet. */
    readonly role: Role;
    /** The path of the page, without its query nor its fragment. */
    readonly page: string;
    /** The client build of the page, null outside of `npm run build`. */
    readonly buildId: string | null;
    /** The clock the limits are counted with, in milliseconds. */
    readonly now?: () => number;
}

/**
 * Reports the errors of a page to the server, which logs them for the operator: nothing ever shows
 * on screen. An error repeated in a loop (in a rendering, for instance) is reported once a minute,
 * and a page never sends more than 10 reports a minute. While the page is disconnected, its reports
 * wait, 20 at most, and are sent once it is back.
 *
 * Reporting never throws, nor rejects: a report that cannot be sent never causes another.
 */
export class ErrorReporter {
    readonly #options: ErrorReporterOptions;
    readonly #now: () => number;
    /** When each error was last reported, by `keyOf`. */
    readonly #lastReported = new Map<string, number>();
    /** When each report of the current window was made. */
    #reportedAt: number[] = [];
    #queue: ClientErrorReport[] = [];
    #connection: GameConnection | null = null;
    #connected = false;
    #shown: () => ShownSnapshot = () => nothingShown;

    constructor(options: ErrorReporterOptions) {
        this.#options = options;
        this.#now = options.now ?? (() => Date.now());
    }

    /** The reports waiting for the connection, oldest first. */
    get queued(): readonly ClientErrorReport[] {
        return this.#queue;
    }

    /**
     * Reports `error`, of any type, unless it is noise or was reported too recently. `source` is
     * the script the browser blames for it, when it tells.
     */
    report(kind: ClientErrorKind, error: unknown, source: string | null = null): void {
        try {
            const description = describeError(error);
            if (isIgnoredError(description, source) || !this.#admit(description)) {
                return;
            }
            this.#send(this.#build(kind, description));
        } catch {
            // Reporting must never raise an error of its own, which would be reported in turn.
        }
    }

    /**
     * Sends the reports through `connection` from then on, those waiting first, and adds to each
     * what `shown` tells of the page. To call before the connection starts, so as to know when it
     * is established. Returns a function that stops sending: reports wait again.
     */
    connect(connection: GameConnection, shown: () => ShownSnapshot): () => void {
        this.#connection = connection;
        this.#connected = false;
        this.#shown = shown;
        const isCurrent = () => this.#connection === connection;
        connection.onConnected(() => {
            if (isCurrent()) {
                this.#connected = true;
                this.#flush();
            }
        });
        connection.onReconnecting(() => {
            if (isCurrent()) {
                this.#connected = false;
            }
        });
        return () => {
            if (isCurrent()) {
                this.#connection = null;
                this.#connected = false;
                this.#shown = () => nothingShown;
            }
        };
    }

    /** Whether an error may be reported now, counting it if so. */
    #admit(description: ErrorDescription): boolean {
        const now = this.#now();
        const windowStart = now - reportLimits.windowMs;
        this.#reportedAt = this.#reportedAt.filter((at) => at > windowStart);
        for (const [key, at] of this.#lastReported) {
            if (at <= windowStart) {
                this.#lastReported.delete(key);
            }
        }

        const key = keyOf(description);
        if (
            this.#lastReported.has(key) ||
            this.#reportedAt.length >= reportLimits.reportsPerWindow
        ) {
            return false;
        }
        this.#lastReported.set(key, now);
        this.#reportedAt.push(now);
        return true;
    }

    #build(kind: ClientErrorKind, { message, stack }: ErrorDescription): ClientErrorReport {
        const { role, page, buildId } = this.#options;
        // What the page showed when the error happened, not when the report leaves.
        const shown = this.#shown();
        return {
            role,
            page: truncate(page, reportFieldLimits.page),
            kind,
            message: truncate(message, reportFieldLimits.message),
            stack: stack === null ? null : truncate(stack, reportFieldLimits.stack),
            roundViewType:
                shown.roundViewType === null
                    ? null
                    : truncate(shown.roundViewType, reportFieldLimits.roundViewType),
            snapshotVersion: shown.version,
            buildId: buildId === null ? null : truncate(buildId, reportFieldLimits.buildId),
        };
    }

    #send(report: ClientErrorReport): void {
        const connection = this.#connection;
        if (connection === null || !this.#connected) {
            this.#enqueue(report);
            return;
        }
        connection.invoke('ReportClientError', report).catch(() => {
            // Lost on the way, most likely with the connection: it goes out again once it is back.
            this.#enqueue(report);
        });
    }

    #enqueue(report: ClientErrorReport): void {
        // The first errors of an outage tell the most: those beyond the limit are dropped.
        if (this.#queue.length < reportLimits.queued) {
            this.#queue.push(report);
        }
    }

    #flush(): void {
        const waiting = this.#queue;
        this.#queue = [];
        for (const report of waiting) {
            this.#send(report);
        }
    }
}

/** Two errors with the same message and the same start of stack trace are the same error. */
function keyOf({ message, stack }: ErrorDescription): string {
    return `${message}\n${(stack ?? '').slice(0, 300)}`;
}
