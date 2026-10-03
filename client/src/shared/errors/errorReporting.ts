import type { ClientErrorKind, Role } from '../contracts';
import type { GameConnection } from '../connection/gameHub';
import type { SnapshotStore, VersionedSnapshot } from '../connection/snapshotStore.svelte';
import { ErrorReporter } from './errorReporter';
import { catchUncaughtErrors } from './uncaughtErrors';

/** The reporter of the page, once `startErrorReporting` ran. */
let reporter: ErrorReporter | null = null;

/**
 * Reports to the server every error the page does not catch, as `role`. To call first in the
 * entry point of the page, before mounting it, so that an error of the first rendering is
 * reported too: it waits for `connectErrorReporting`.
 */
export function startErrorReporting(role: Role): void {
    if (reporter !== null) {
        return;
    }
    const pageReporter = new ErrorReporter({
        role,
        page: location.pathname,
        buildId: __PARTYGAME_BUILD_ID__,
    });
    reporter = pageReporter;
    catchUncaughtErrors((kind, error, source) => pageReporter.report(kind, error, source));
}

/**
 * Reports an error the page caught itself, such as the rendering of a view that failed. Does
 * nothing before `startErrorReporting`, and never throws.
 */
export function reportError(kind: ClientErrorKind, error: unknown): void {
    reporter?.report(kind, error);
}

/** What every snapshot tells about the round view it shows. */
interface ShowingSnapshot extends VersionedSnapshot {
    readonly roundView: { readonly type: string } | null;
}

/**
 * Sends the reports of the page through `connection`, with the snapshot `game` shows. To call
 * before the connection starts. Returns a function that stops sending.
 */
export function connectErrorReporting(
    connection: GameConnection,
    game: SnapshotStore<ShowingSnapshot>,
): () => void {
    return (
        reporter?.connect(connection, () => ({
            version: game.current?.version ?? null,
            roundViewType: game.current?.roundView?.type ?? null,
        })) ?? (() => {})
    );
}
