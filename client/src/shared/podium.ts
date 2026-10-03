/** The ranks that stand on the podium of the final ranking. */
export const podiumRankCount = 3;

/** Whether a rank received from the server stands on the podium, ex aequo or not. */
export function isOnPodium(rank: number): boolean {
    return rank <= podiumRankCount;
}

/** A step of the podium: a rank, and every player who holds it. */
export interface PodiumStep<P> {
    readonly rank: number;
    /** In the order of the ranking: ex aequo share their step, in alphabetical order. */
    readonly players: readonly P[];
}

/** The final ranking as the TV screen shows it: a podium, then the rest of the players. */
export interface FinalRanking<P> {
    /** The ranks of the podium some player holds, first rank first. */
    readonly steps: readonly PodiumStep<P>[];
    /** The players below the podium, in the order of the ranking. */
    readonly rest: readonly P[];
}

/**
 * Splits a ranking as the server sends it between its podium and the rest. The server ranks the
 * players: this neither sorts nor ranks anything, it only groups the players of each rank of the
 * podium. A rank the ex aequo above skip (1, 1, 3) has no step.
 */
export function splitFinalRanking<P extends { readonly rank: number }>(
    ranking: readonly P[],
): FinalRanking<P> {
    const steps: { rank: number; players: P[] }[] = [];
    // Ranked by the server: the players of a rank follow each other.
    for (const player of ranking.filter((p) => isOnPodium(p.rank))) {
        const last = steps[steps.length - 1];
        if (last?.rank === player.rank) {
            last.players.push(player);
        } else {
            steps.push({ rank: player.rank, players: [player] });
        }
    }
    return { steps, rest: ranking.filter((p) => !isOnPodium(p.rank)) };
}

/**
 * Where a step stands on the podium from left to right: the first in the middle, the second on
 * its left and the third on its right, as on a sports podium.
 */
export function podiumPosition(rank: number): number {
    return rank === 1 ? 2 : rank === 2 ? 1 : 3;
}
