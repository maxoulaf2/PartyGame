import type { Environment } from 'vitest/runtime';

/**
 * Plain Node, like the `node` environment, but with modules transformed for the browser, as jsdom
 * does without its DOM: Svelte compiles `.svelte.ts` modules for the client, where effects run,
 * rather than for the server, where they never do.
 */
export default {
    name: 'svelte-client',
    viteEnvironment: 'client',
    setup: () => ({ teardown: () => {} }),
} satisfies Environment;
