import { describe, expect, it } from 'vitest';
import type { GameId, GameMasterSavedGame, RoundId } from '../shared/contracts';
import { savedGameProgressText } from './savedGameText';

const finale = {
    roundId: '0f8fad5b-d9cb-469f-a165-70867728950e' as RoundId,
    number: 2,
    count: 3,
    title: 'Finale',
    mode: 'quiz',
    description: null,
};

function found(game: Partial<GameMasterSavedGame>): GameMasterSavedGame {
    return {
        gameId: '6f9619ff-8b86-d011-b42d-00cf4fc964ff' as GameId,
        savedAt: 0,
        phase: 'Lobby',
        packTitle: null,
        round: null,
        step: null,
        playerCount: 3,
        missingMedia: [],
        ...game,
    };
}

describe('savedGameProgressText', () => {
    it('tells the lobby', () => {
        expect(savedGameProgressText(found({}))).toBe('Lobby');
    });

    it('tells the round and the step a game stopped in', () => {
        expect(
            savedGameProgressText(
                found({ phase: 'Round', round: finale, step: { number: 4, count: 10 } }),
            ),
        ).toBe('Manche 2/3 · Finale · Question 4/10');
    });

    it('tells the round alone when its mode tells no step', () => {
        expect(savedGameProgressText(found({ phase: 'Round', round: finale }))).toBe(
            'Manche 2/3 · Finale',
        );
    });

    it('tells the end of a round and of the game', () => {
        expect(savedGameProgressText(found({ phase: 'BetweenRounds', round: finale }))).toBe(
            'Fin de la manche 2/3 · Finale',
        );
        expect(savedGameProgressText(found({ phase: 'Finished', round: finale }))).toBe(
            'Partie terminée',
        );
    });
});
