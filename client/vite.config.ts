import { fileURLToPath } from 'node:url';
import { svelte } from '@sveltejs/vite-plugin-svelte';
import { defineConfig } from 'vitest/config';
import { buildIdentifier } from './vite/buildIdentifier.ts';
import { canonicalPages } from './vite/canonicalPages.ts';

const page = (path: string): string => fileURLToPath(new URL(path, import.meta.url));

// Where `dotnet run --project src/PartyGame.Server` listens by default.
const server = process.env.PARTYGAME_SERVER_URL ?? 'http://localhost:5000';

export default defineConfig({
    plugins: [svelte(), canonicalPages(['display', 'gm', 'diagnostic']), buildIdentifier()],
    // Tests load the client build of Svelte, as the pages do (see test.environment).
    resolve: process.env.VITEST ? { conditions: ['browser'] } : undefined,
    build: {
        // The .NET server serves the client from its web root.
        outDir: page('../src/PartyGame.Server/wwwroot'),
        emptyOutDir: true,
        rollupOptions: {
            // One HTML page per role, and the network diagnostic. Their location gives the URLs /,
            // /display/, /gm/ and /diagnostic/.
            input: {
                player: page('./index.html'),
                display: page('./display/index.html'),
                gm: page('./gm/index.html'),
                diagnostic: page('./diagnostic/index.html'),
            },
        },
    },
    server: {
        // Listen on every interface so that phones on the local network can open the dev pages.
        host: true,
        port: 5173,
        strictPort: true,
        // Also used by `vite preview`.
        proxy: {
            '/api': { target: server },
            '/hub': { target: server, ws: true },
            '/media': { target: server },
        },
    },
    test: {
        include: ['src/**/*.test.ts', 'eslint/**/*.test.ts'],
        // Runes behave as in the pages, effects included: modules are compiled for the client, and
        // `svelte` is resolved by Vite with the browser condition rather than imported by Node,
        // which would load its server build, where effects never run.
        environment: './vite/svelteClientEnvironment.ts',
        server: { deps: { inline: ['svelte'] } },
    },
});
