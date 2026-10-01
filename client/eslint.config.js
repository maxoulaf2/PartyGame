import js from '@eslint/js';
import svelte from 'eslint-plugin-svelte';
import globals from 'globals';
import ts from 'typescript-eslint';
import svelteConfig from './svelte.config.js';

const signalrOnlyInConnection =
    'Talk to the server through src/shared/connection: it is the only module that uses SignalR.';

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
            'no-restricted-imports': [
                'error',
                {
                    paths: [{ name: '@microsoft/signalr', message: signalrOnlyInConnection }],
                    patterns: [
                        { group: ['@microsoft/signalr/*'], message: signalrOnlyInConnection },
                    ],
                },
            ],
        },
    },
    {
        // The only module allowed to talk to SignalR directly.
        files: ['src/shared/connection/**'],
        rules: {
            'no-restricted-imports': 'off',
        },
    },
);
