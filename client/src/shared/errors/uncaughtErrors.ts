import type { ClientErrorKind } from '../contracts';

/** Reports an error caught by the page, with the script the browser blames for it, if any. */
export type ErrorSink = (kind: ClientErrorKind, error: unknown, source: string | null) => void;

/** What the `error` event of `window` tells, an `ErrorEvent`. */
interface UncaughtError {
    readonly error?: unknown;
    readonly message?: unknown;
    readonly filename?: unknown;
}

/** What the `unhandledrejection` event of `window` tells, a `PromiseRejectionEvent`. */
interface UnhandledRejection {
    readonly reason?: unknown;
}

/**
 * Hands `sink` every error nothing caught on the page, those of event handlers and asynchronous
 * code included, which no `<svelte:boundary>` sees, and every promise rejected without a handler.
 * The browser still logs them in its console; nothing shows on screen. Returns a function that
 * stops listening.
 */
export function catchUncaughtErrors(sink: ErrorSink, target: EventTarget = window): () => void {
    const onError = (event: Event) => {
        const { error, message, filename } = event as UncaughtError;
        // Without `error`, as for a script of another origin, the message is all there is.
        sink(
            'Error',
            error ?? message,
            typeof filename === 'string' && filename !== '' ? filename : null,
        );
    };
    const onRejection = (event: Event) => {
        sink('UnhandledRejection', (event as UnhandledRejection).reason, null);
    };
    target.addEventListener('error', onError);
    target.addEventListener('unhandledrejection', onRejection);
    return () => {
        target.removeEventListener('error', onError);
        target.removeEventListener('unhandledrejection', onRejection);
    };
}
