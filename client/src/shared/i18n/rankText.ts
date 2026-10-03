import { fill } from './fill';
import { formatNumber } from './numberText';

/** The forms of a rank written the French way, `{rank}` standing for the number. */
export interface RankTexts {
    /** The first rank, whose ordinal is irregular (« 1er »). */
    readonly first: string;
    readonly other: string;
}

/** Where a player stands among the others, `{rank}` and `{count}` standing for the numbers. */
export interface StandingTexts {
    readonly alone: string;
    /** Several players share the rank. */
    readonly tied: string;
}

/** Writes a rank received from the server as a French ordinal: « 1er », « 2e », « 21e ». */
export function rankText(texts: RankTexts, rank: number): string {
    return fill(rank === 1 ? texts.first : texts.other, { rank: formatNumber(rank) });
}

/**
 * Writes where a player stands out of `count` players, ex aequo when they share their rank: « 3e ex
 * aequo sur 9 ». The server ranks the players: this only writes what it sent.
 */
export function standingText(
    texts: StandingTexts,
    ranks: RankTexts,
    standing: { readonly rank: number; readonly isTied: boolean },
    count: number,
): string {
    return fill(standing.isTied ? texts.tied : texts.alone, {
        rank: rankText(ranks, standing.rank),
        count: formatNumber(count),
    });
}
