import type { GameMasterSavedGame } from '../shared/contracts';
import { fill, roundText } from '../shared/i18n/fill';
import { fr } from '../shared/i18n/fr';

const texts = fr.gm.resume.progress;

/**
 * Where the game found saved stopped, as the console describes it: « Lobby », « Manche 2/3 ·
 * Finale · Question 4/10 », « Partie terminée ».
 */
export function savedGameProgressText(savedGame: GameMasterSavedGame): string {
    const { phase, round, step } = savedGame;
    if (phase === 'Finished') {
        return texts.finished;
    }
    if (round === null) {
        return texts.lobby;
    }
    if (phase === 'BetweenRounds') {
        return fill(roundText(texts.roundEnded, round), { title: round.title });
    }
    const inRound = fill(roundText(texts.round, round), { title: round.title });
    return step === null
        ? inRound
        : `${inRound} · ${fill(texts.step, { number: step.number, count: step.count })}`;
}
