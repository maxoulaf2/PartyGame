/** A player of a ranking as the server sends it, with their rank in the ranking before. */
interface Moved {
    readonly rank: number;
    readonly previousRank: number | null;
}

/**
 * The places a player gained since the previous ranking, negative when they lost some, or null
 * when they were not in it: after the first round, or having joined since.
 */
export function placesGained(player: Moved): number | null {
    return player.previousRank === null ? null : player.previousRank - player.rank;
}

/**
 * Where each player of `ranking` stood in the previous ranking, as an index into the list shown:
 * the rows start there before sliding to their new place. The server ranks the players: this only
 * orders them by the ranks it sent, those who were not ranked last, ties in the order received.
 */
export function previousIndexes(ranking: readonly Moved[]): number[] {
    const before = ranking
        .map((player, index) => ({ index, rank: player.previousRank ?? Infinity }))
        .sort((a, b) => a.rank - b.rank);
    const indexes = new Array<number>(ranking.length);
    before.forEach(({ index }, previous) => (indexes[index] = previous));
    return indexes;
}
