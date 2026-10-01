import { vitePreprocess } from '@sveltejs/vite-plugin-svelte';

/** @type {import('@sveltejs/vite-plugin-svelte').SvelteConfig} */
export default {
    preprocess: vitePreprocess(),
    compilerOptions: {
        // Runes only: legacy reactive syntax ($:, export let) is a compile error.
        runes: true,
    },
};
