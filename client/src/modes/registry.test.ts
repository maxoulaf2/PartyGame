import { describe, expect, it } from 'vitest';
import type { DisplayRoundView, GameMasterRoundView, PlayerRoundView } from '../shared/contracts';
import QuizDisplay from './quiz/QuizDisplay.svelte';
import QuizGameMaster from './quiz/QuizGameMaster.svelte';
import QuizPlayer from './quiz/QuizPlayer.svelte';
import { findDisplayView, findGameMasterView, findPlayerView } from './registry';

/** A view of the given type: the lookup reads its type only. */
function viewOfType<V>(type: string): V {
    return { type } as V;
}

describe('registry', () => {
    it('finds the view of each role for a known mode', () => {
        expect(findPlayerView(viewOfType<PlayerRoundView>('quiz'))).toBe(QuizPlayer);
        expect(findDisplayView(viewOfType<DisplayRoundView>('quiz'))).toBe(QuizDisplay);
        expect(findGameMasterView(viewOfType<GameMasterRoundView>('quiz'))).toBe(QuizGameMaster);
    });

    // As a newer server could send them, of types this build does not know.
    it.each(['karaoke', 'toString', '__proto__', ''])(
        'finds no view for the unknown mode %j',
        (type) => {
            expect(findPlayerView(viewOfType<PlayerRoundView>(type))).toBeNull();
            expect(findDisplayView(viewOfType<DisplayRoundView>(type))).toBeNull();
            expect(findGameMasterView(viewOfType<GameMasterRoundView>(type))).toBeNull();
        },
    );
});
