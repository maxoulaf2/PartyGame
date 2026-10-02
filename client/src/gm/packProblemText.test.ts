import { describe, expect, it } from 'vitest';
import type { PackProblem, PackProblemCode } from '../shared/contracts';
import { describeMode, describeProblem } from './packProblemText';

function problem(code: PackProblemCode, parameters: Record<string, string> = {}): PackProblem {
    return { code, file: 'pack.json', path: '$.rounds[0]', parameters };
}

describe('describeProblem', () => {
    it('fills in the parameters of the message', () => {
        expect(
            describeProblem(problem('PackMediaMissing', { media: 'images/tour-eiffel.jpg' })),
        ).toBe('Média introuvable : images/tour-eiffel.jpg');
        expect(describeProblem(problem('PackJsonInvalid', { line: '12', column: '5' }))).toBe(
            'JSON mal formé, ligne 12, colonne 5 : vérifiez les virgules, guillemets et accolades autour.',
        );
    });

    it('shows a parameter as written, even with replacement patterns', () => {
        expect(describeProblem(problem('QuizChoiceDuplicated', { choice: '$& et $1' }))).toBe(
            'Proposition en double : « $& et $1 »',
        );
    });

    it('says in words which type of value is expected', () => {
        expect(describeProblem(problem('PackValueTypeInvalid', { expected: 'integer' }))).toBe(
            'Valeur incorrecte : il faut un nombre entier.',
        );
        expect(describeProblem(problem('PackValueTypeInvalid', { expected: 'mystery' }))).toBe(
            'Valeur incorrecte : il faut mystery.',
        );
    });

    it('words a bound by the limits it has', () => {
        expect(describeProblem(problem('PackItemCountOutOfRange', { min: '2', max: '4' }))).toBe(
            'Nombre d’éléments incorrect : de 2 à 4',
        );
        expect(describeProblem(problem('PackItemCountOutOfRange', { min: '1' }))).toBe(
            'Pas assez d’éléments : au moins 1',
        );
        expect(describeProblem(problem('PackItemCountOutOfRange', { max: '50' }))).toBe(
            'Trop d’éléments : au plus 50',
        );
        // StringLength without a minimum length gives a minimum of 0, which bounds nothing.
        expect(describeProblem(problem('PackTextLengthOutOfRange', { min: '0', max: '200' }))).toBe(
            'Texte trop long : longueur maximale 200',
        );
        expect(describeProblem(problem('PackTextLengthOutOfRange', { min: '1', max: '60' }))).toBe(
            'Longueur du texte incorrecte : de 1 à 60 caractères',
        );
    });

    it('leaves a missing parameter visible rather than blank', () => {
        expect(describeProblem(problem('PackMediaMissing'))).toBe('Média introuvable : {media}');
    });
});

describe('describeMode', () => {
    it('names a known mode in French, and shows an unknown one as is', () => {
        expect(describeMode('quiz')).toBe('Quiz QCM');
        expect(describeMode('karaoke')).toBe('karaoke');
    });
});
