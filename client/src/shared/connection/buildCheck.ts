import { reloadTargetKey, sessionCodeStorage, type CodeStorage } from './codeStorage';
import type { GameConnection } from './gameHub';

/**
 * What a page does with the build the server serves:
 * - `keep`: nothing to do, the builds agree or one of them is unknown;
 * - `reload`: the page is outdated, a reload fetches the build served;
 * - `report`: the page is still outdated after reloading for that build: it goes on as it is.
 */
export type BuildDecision = 'keep' | 'reload' | 'report';

/**
 * Decides what a page built as `pageBuild` does when the server serves `serverBuild`, given the
 * build it last reloaded to get. A null build is unknown: a page of `npm run dev`, a server
 * without client build. Neither ever triggers a reload.
 */
export function decideOnBuild(
    pageBuild: string | null,
    serverBuild: string | null,
    reloadedFor: string | null,
): BuildDecision {
    if (pageBuild === null || serverBuild === null || pageBuild === serverBuild) {
        return 'keep';
    }
    return reloadedFor === serverBuild ? 'report' : 'reload';
}

/** What `watchBuild` relies on, so that tests can stand in for the page. */
export interface BuildWatchOptions {
    /** The build of the page, null outside of `npm run build`. */
    pageBuild: string | null;
    /** Where the build the page reloaded to get is kept across the reload. */
    reloadTarget: CodeStorage;
    /** Reloads the page. */
    reload: () => void;
}

const pageOptions = (): BuildWatchOptions => ({
    pageBuild: __PARTYGAME_BUILD_ID__,
    reloadTarget: sessionCodeStorage(reloadTargetKey),
    reload: () => location.reload(),
});

/**
 * Reloads the page, without asking anything, when the server tells it serves another build: the
 * page was cached by the browser before an update of the server. A player finds their place back
 * after the reload thanks to their token.
 *
 * The page reloads once per build served: if the reload brings back the same outdated page (a
 * stubborn cache, a proxy), it goes on as it is and reports it to the server, once.
 *
 * Inactive outside of `npm run build`. To call before the connection starts, so as not to miss
 * the welcome of the server. Returns a function that stops watching.
 */
export function watchBuild(
    connection: GameConnection,
    options: BuildWatchOptions = pageOptions(),
): () => void {
    const { pageBuild, reloadTarget, reload } = options;
    if (pageBuild === null) {
        return () => {};
    }

    let reported = false;
    const report = () => {
        if (reported) {
            return;
        }
        reported = true;
        connection.invoke('ReportStaleBuild', { clientBuildId: pageBuild }).catch(() => {
            // The connection dropped meanwhile: the next welcome reports it again.
            reported = false;
        });
    };

    return connection.on('ReceiveWelcome', ({ buildId }) => {
        const decision = decideOnBuild(pageBuild, buildId, reloadTarget.load());
        if (decision === 'keep') {
            reloadTarget.clear();
        } else if (decision === 'reload' && buildId !== null) {
            reloadTarget.save(buildId);
            if (reloadTarget.load() === buildId) {
                reload();
            } else {
                // Without storage, nothing would stop a reload loop: the page goes on as it is.
                report();
            }
        } else {
            report();
        }
    });
}
