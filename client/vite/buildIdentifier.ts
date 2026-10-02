import { randomBytes } from 'node:crypto';
import type { Plugin } from 'vite';

/** The file of the build, next to the pages, that the .NET server reads at startup (`FrontEndBuild`). */
export const buildFileName = 'build.json';

/**
 * Identifies each `npm run build`: the identifier is compiled into the pages as
 * `__PARTYGAME_BUILD_ID__` and written to `build.json` for the server, which tells it to every
 * connection. A page left open on another build then reloads itself. Without a build (`npm run
 * dev`, Vitest), the identifier is null and the check is inactive.
 */
export function buildIdentifier(): Plugin {
    let buildId: string | null = null;
    return {
        name: 'partygame-build-identifier',
        config: (_, { command }) => {
            buildId = command === 'build' ? createBuildId() : null;
            return { define: { __PARTYGAME_BUILD_ID__: JSON.stringify(buildId) } };
        },
        generateBundle() {
            if (buildId !== null) {
                this.emitFile({
                    type: 'asset',
                    fileName: buildFileName,
                    source: `${JSON.stringify({ buildId })}\n`,
                });
            }
        },
    };
}

/** When the build was made, readable in the logs of the server, and a random part to tell apart two builds in the same millisecond. */
function createBuildId(): string {
    return `${new Date().toISOString().replace(/[:.]/g, '-')}-${randomBytes(3).toString('hex')}`;
}
