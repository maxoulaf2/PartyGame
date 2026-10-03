import path from 'node:path';

const modesDirectory = path.resolve(import.meta.dirname, '../src/modes');
const sharedDirectory = path.resolve(import.meta.dirname, '../src/shared');

/** Whether `target` is `directory` or lies within it. */
function isWithin(target, directory) {
    const relative = path.relative(directory, target);
    return !relative.startsWith('..') && !path.isAbsolute(relative);
}

/**
 * Keeps each game mode apart: a file of `src/modes/<mode>/` imports nothing of the project but
 * `src/shared/` and its own folder, never another mode nor a page. Packages stay allowed, SignalR
 * aside, which `no-restricted-imports` keeps to `src/shared/connection`. The registry, at the root
 * of `src/modes/`, is the one place that knows every mode.
 *
 * Paths are resolved rather than matched as text, so that a mode may nest folders freely.
 *
 * @type {import('eslint').Rule.RuleModule}
 */
const rule = {
    meta: {
        type: 'problem',
        docs: {
            description: 'Restrict the imports of a game mode to src/shared and its own folder',
        },
        messages: {
            outside:
                "A game mode depends only on src/shared and its own folder: '{{source}}' is out of bounds.",
        },
        schema: [],
    },
    create(context) {
        const file = path.resolve(context.filename);
        const [mode, ...rest] = path.relative(modesDirectory, file).split(path.sep);
        if (!isWithin(file, modesDirectory) || mode === undefined || rest.length === 0) {
            return {};
        }
        const modeDirectory = path.join(modesDirectory, mode);

        /** @param {{ source?: import('estree').Node | null }} node */
        function check(node) {
            const source = node.source;
            if (source?.type !== 'Literal' || typeof source.value !== 'string') {
                return;
            }
            const specifier = source.value;
            if (!specifier.startsWith('.') && !specifier.startsWith('/')) {
                return;
            }
            const target = path.resolve(path.dirname(file), specifier);
            if (!isWithin(target, sharedDirectory) && !isWithin(target, modeDirectory)) {
                context.report({ node: source, messageId: 'outside', data: { source: specifier } });
            }
        }

        return {
            ImportDeclaration: check,
            ImportExpression: check,
            ExportAllDeclaration: check,
            ExportNamedDeclaration: check,
        };
    },
};

/** The rules of the project itself. */
export const partygame = {
    meta: { name: 'partygame' },
    rules: { 'mode-boundaries': rule },
};
