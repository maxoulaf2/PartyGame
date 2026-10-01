import { fileURLToPath } from 'node:url';
import { svelte } from '@sveltejs/vite-plugin-svelte';
import { defineConfig } from 'vitest/config';

const page = (path: string): string => fileURLToPath(new URL(path, import.meta.url));

// Where `dotnet run --project src/PartyGame.Server` listens by default.
const server = process.env.PARTYGAME_SERVER_URL ?? 'http://localhost:5000';

export default defineConfig({
    plugins: [svelte()],
    build: {
        // The .NET server serves the client from its web root.
        outDir: page('../src/PartyGame.Server/wwwroot'),
        emptyOutDir: true,
        rollupOptions: {
            // One HTML page per role. Their location gives the URLs /, /display/ and /gm/.
            input: {
                player: page('./index.html'),
                display: page('./display/index.html'),
                gm: page('./gm/index.html'),
            },
        },
    },
    server: {
        // Listen on every interface so that phones on the local network can open the dev pages.
        host: true,
        port: 5173,
        strictPort: true,
        proxy: {
            '/hub': { target: server, ws: true },
            '/media': { target: server },
        },
    },
    test: {
        include: ['src/**/*.test.ts'],
    },
});
