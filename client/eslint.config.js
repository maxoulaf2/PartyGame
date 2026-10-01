import js from '@eslint/js';
import svelte from 'eslint-plugin-svelte';
import globals from 'globals';
import ts from 'typescript-eslint';
import svelteConfig from './svelte.config.js';

export default ts.config(
    {
        ignores: ['dist/', 'playwright-report/', 'test-results/', 'src/shared/contracts/'],
    },
    js.configs.recommended,
    ...ts.configs.recommended,
    ...svelte.configs.recommended,
    {
        languageOptions: {
            globals: { ...globals.browser, ...globals.node },
        },
    },
    {
        files: ['**/*.svelte', '**/*.svelte.ts'],
        languageOptions: {
            parserOptions: {
                parser: ts.parser,
                extraFileExtensions: ['.svelte'],
                svelteConfig,
            },
        },
    },
    {
        rules: {
            '@typescript-eslint/no-explicit-any': 'error',
            '@typescript-eslint/ban-ts-comment': [
                'error',
                {
                    'ts-ignore': 'allow-with-description',
                    'ts-expect-error': 'allow-with-description',
                    'ts-nocheck': true,
                    minimumDescriptionLength: 10,
                },
            ],
        },
    },
);
