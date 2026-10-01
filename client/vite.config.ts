import { fileURLToPath } from 'node:url';
import { svelte } from '@sveltejs/vite-plugin-svelte';
import { defineConfig } from 'vitest/config';

const page = (path: string): string => fileURLToPath(new URL(path, import.meta.url));

export default defineConfig({
    plugins: [svelte()],
    build: {
        rollupOptions: {
            // One HTML page per role. Their location gives the dev URLs /, /display/ and /gm/.
            input: {
                player: page('./index.html'),
                display: page('./display/index.html'),
                gm: page('./gm/index.html'),
            },
        },
    },
    test: {
        include: ['src/**/*.test.ts'],
    },
});
