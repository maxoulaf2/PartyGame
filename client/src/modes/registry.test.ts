import { describe, expect, it } from 'vitest';
import type { DisplayRoundView, GameMasterRoundView, PlayerRoundView } from '../shared/contracts';
import QuizDisplay from './quiz/QuizDisplay.svelte';
import QuizGameMaster from './quiz/QuizGameMaster.svelte';
import QuizPlayer from './quiz/QuizPlayer.svelte';
import { findDisplayView, findGameMasterView, findPlayerView } from './registry';

/** A view as a newer server could send it, of a type this build does not know. */
function viewOfType<V>(type: string): V {
    return { type } as V;
}

describe('registry', () => {
    it('finds the view of each role for a known mode', () => {
        expect(findPlayerView({ type: 'quiz' })).toBe(QuizPlayer);
        expect(findDisplayView({ type: 'quiz' })).toBe(QuizDisplay);
        expect(findGameMasterView({ type: 'quiz' })).toBe(QuizGameMaster);
    });

    it.each(['blindtest', 'toString', '__proto__', ''])(
        'finds no view for the unknown mode %j',
        (type) => {
            expect(findPlayerView(viewOfType<PlayerRoundView>(type))).toBeNull();
            expect(findDisplayView(viewOfType<DisplayRoundView>(type))).toBeNull();
            expect(findGameMasterView(viewOfType<GameMasterRoundView>(type))).toBeNull();
        },
    );
});
