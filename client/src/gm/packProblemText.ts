import type { PackProblem, PackProblemCode } from '../shared/contracts';
import { fr } from '../shared/i18n/fr';

/** The forms of the message of a bound, depending on which of `min` and `max` it has. */
interface BoundTexts {
    readonly between: string;
    readonly atLeast: string;
    readonly atMost: string;
}

// Typed by the generated codes: a code added on the server without its message fails the check.
const problemTexts: Readonly<Record<PackProblemCode, string | BoundTexts>> = fr.gm.packs.problems;

const valueTypes: Readonly<Record<string, string>> = fr.gm.packs.valueTypes;

/**
 * The message of a pack problem, in French, with its parameters filled in, for the game master to
 * fix the pack. A bound of 0 is no bound: a text of at most 200 characters reads as such.
 */
export function describeProblem(problem: PackProblem): string {
    const parameters = problem.parameters;
    const text = problemTexts[problem.code];
    const template = typeof text === 'string' ? text : boundText(text, parameters);
    // A function as replacement: a parameter such as « $& » must show as written in the pack.
    return template.replace(/\{(\w+)\}/g, (placeholder, name: string) => {
        const value = parameters[name];
        if (value === undefined) {
            return placeholder;
        }
        return name === 'expected' ? (valueTypes[value] ?? value) : value;
    });
}

/** The name of a game mode, by the type of activity that designates it in the packs. */
export function describeMode(mode: string): string {
    const modes: Readonly<Record<string, string>> = fr.gm.packs.modes;
    return modes[mode] ?? mode;
}

function boundText(texts: BoundTexts, parameters: Readonly<Record<string, string>>): string {
    const hasMin = parameters.min !== undefined && parameters.min !== '0';
    const hasMax = parameters.max !== undefined;
    if (hasMin && hasMax) {
        return texts.between;
    }
    return hasMin ? texts.atLeast : texts.atMost;
}
