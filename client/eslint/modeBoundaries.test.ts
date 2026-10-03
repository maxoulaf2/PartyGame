// @vitest-environment node
import { fileURLToPath } from 'node:url';
import { ESLint } from 'eslint';
import { beforeAll, describe, expect, it } from 'vitest';

const root = fileURLToPath(new URL('..', import.meta.url));
const eslint = new ESLint({ cwd: root });

/** The import rules `code` breaks, linted with the configuration of the project as `file`. */
async function brokenImportRules(file: string, code: string): Promise<string[]> {
    const [result] = await eslint.lintText(code, { filePath: `${root}/${file}` });
    return (result?.messages ?? [])
        .map((message) => message.ruleId ?? 'parsing')
        .filter((rule) => /import|boundaries|parsing/.test(rule));
}

describe('mode-boundaries', () => {
    // The first lint loads the configuration and its plugins: seconds, once for every test.
    beforeAll(() => brokenImportRules('src/modes/quiz/view.ts', ''), 60_000);

    it.each([
        ['src/shared', "import '../../shared/i18n/fr';"],
        ['its own folder', "import './QuizChoice.svelte';"],
        ['a package', "import 'svelte';"],
    ])('lets a mode import %s', async (_, code) => {
        expect(await brokenImportRules('src/modes/quiz/view.ts', code)).toEqual([]);
    });

    it.each([
        ['another mode', "import '../blindtest/view';"],
        ['the player page', "import '../../player/LobbyScreen.svelte';"],
        ['the TV page', "import '../../display/LobbyScreen.svelte';"],
        ['the GM page', "export * from '../../gm/packProblemText';"],
        ['the registry', "import '../registry';"],
        ['a page lazily', "void import('../../gm/App.svelte');"],
    ])('refuses that a mode imports %s', async (_, code) => {
        expect(await brokenImportRules('src/modes/quiz/view.ts', code)).toEqual([
            'partygame/mode-boundaries',
        ]);
    });

    it('refuses that a mode imports SignalR', async () => {
        const code = "import '@microsoft/signalr';";

        expect(await brokenImportRules('src/modes/quiz/view.ts', code)).toEqual([
            'no-restricted-imports',
        ]);
    });

    it('resolves the imports of nested folders', async () => {
        const allowed = "import '../../../shared/theme.css';\nimport '../view';";
        const refused = "import '../../blindtest/view';";

        expect(await brokenImportRules('src/modes/quiz/parts/choice.ts', allowed)).toEqual([]);
        expect(await brokenImportRules('src/modes/quiz/parts/choice.ts', refused)).toEqual([
            'partygame/mode-boundaries',
        ]);
    });

    it('checks the components of a mode', async () => {
        const code = `<script lang="ts">\n    import '../../gm/App.svelte';\n</script>\n`;

        expect(await brokenImportRules('src/modes/quiz/View.svelte', code)).toEqual([
            'partygame/mode-boundaries',
        ]);
    });

    it('lets the registry import every mode, and the pages import the registry', async () => {
        const registry = "import './quiz/view';";
        const page = "import '../modes/registry';";

        expect(await brokenImportRules('src/modes/registry.ts', registry)).toEqual([]);
        expect(await brokenImportRules('src/player/screen.ts', page)).toEqual([]);
    });
});
