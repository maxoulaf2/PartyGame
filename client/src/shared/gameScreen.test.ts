import { describe, expect, it } from 'vitest';
import type { Phase, RoundId, RoundInfo } from './contracts';
import { selectGameScreen, type GameProgress } from './gameScreen';

interface View {
    readonly type: string;
}

const round: RoundInfo = {
    roundId: '0f8fad5b-d9cb-469f-a165-70867728950e' as RoundId,
    number: 1,
    count: 2,
    title: 'Échauffement',
    mode: 'quiz',
    description: null,
};

const quizView: View = { type: 'quiz' };

/** Knows the quiz only, as a build without any other mode would. */
const findView = (view: View): string | null => (view.type === 'quiz' ? 'QuizView' : null);

function progress(
    phase: Phase,
    current: RoundInfo | null = null,
    roundView: View | null = null,
): GameProgress<View> {
    return { phase, round: current, roundView };
}

describe('selectGameScreen', () => {
    it('shows the lobby before the game starts', () => {
        expect(selectGameScreen(progress('Lobby'), findView)).toEqual({ kind: 'lobby' });
    });

    it('shows a round with the view of its mode, when the mode is known', () => {
        expect(selectGameScreen(progress('Round', round, quizView), findView)).toEqual({
            kind: 'round',
            round,
            view: quizView,
            component: 'QuizView',
        });
    });

    it('waits neutrally on a round of a mode the page does not know', () => {
        const screen = selectGameScreen(progress('Round', round, { type: 'blindtest' }), findView);

        expect(screen).toEqual({ kind: 'waiting' });
    });

    it('waits neutrally on a round without its view or its information', () => {
        expect(selectGameScreen(progress('Round', round, null), findView)).toEqual({
            kind: 'waiting',
        });
        expect(selectGameScreen(progress('Round', null, quizView), findView)).toEqual({
            kind: 'waiting',
        });
    });

    it('shows the introduction of the round announced, without any view', () => {
        expect(selectGameScreen(progress('RoundIntro', round), findView)).toEqual({
            kind: 'roundIntro',
            round,
        });
    });

    it('waits neutrally on an introduction without the round announced', () => {
        expect(selectGameScreen(progress('RoundIntro'), findView)).toEqual({ kind: 'waiting' });
    });

    it('shows the end of the round that just finished between two rounds', () => {
        expect(selectGameScreen(progress('BetweenRounds', round), findView)).toEqual({
            kind: 'betweenRounds',
            round,
        });
    });

    it('waits neutrally between two rounds without the round that just finished', () => {
        expect(selectGameScreen(progress('BetweenRounds'), findView)).toEqual({ kind: 'waiting' });
    });

    it('shows the end of the game once it is finished', () => {
        expect(selectGameScreen(progress('Finished', round), findView)).toEqual({
            kind: 'finished',
        });
    });

    it('waits neutrally on a phase of a newer server', () => {
        const phase = 'Paused' as Phase;

        expect(selectGameScreen(progress(phase, round, quizView), findView)).toEqual({
            kind: 'waiting',
        });
    });
});
