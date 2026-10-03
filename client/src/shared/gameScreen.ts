import type { Phase, RoundInfo } from './contracts';

/** What the snapshot of every role tells about the progress of the game. */
export interface GameProgress<V extends { readonly type: string }> {
    readonly phase: Phase;
    readonly round: RoundInfo | null;
    readonly roundView: V | null;
}

/**
 * The screen a page shows for its snapshot, whatever its role:
 * - `lobby`: the game has not started;
 * - `round`: a round is in progress, shown by `component`, the view of its mode for the role;
 * - `betweenRounds`: `round` just finished, and the next one waits for the game master;
 * - `finished`: the last round is over;
 * - `waiting`: nothing the page knows how to show, such as a mode it does not know: the neutral
 *   waiting screen, never a technical message.
 */
export type GameScreen<V, C> =
    | { readonly kind: 'lobby' }
    | {
          readonly kind: 'round';
          readonly round: RoundInfo;
          readonly view: V;
          readonly component: C;
      }
    | { readonly kind: 'betweenRounds'; readonly round: RoundInfo }
    | { readonly kind: 'finished' }
    | { readonly kind: 'waiting' };

const waiting = { kind: 'waiting' } as const;

/**
 * Picks the screen of `snapshot`. `findView` gives the component that shows a view of a round for
 * the role of the page, or null for a mode the page does not know.
 */
export function selectGameScreen<V extends { readonly type: string }, C>(
    snapshot: GameProgress<V>,
    findView: (view: V) => C | null,
): GameScreen<V, C> {
    const { round, roundView } = snapshot;
    switch (snapshot.phase) {
        case 'Lobby':
            return { kind: 'lobby' };
        case 'Round': {
            const component = round && roundView ? findView(roundView) : null;
            return round && roundView && component !== null
                ? { kind: 'round', round, view: roundView, component }
                : waiting;
        }
        case 'BetweenRounds':
            return round ? { kind: 'betweenRounds', round } : waiting;
        case 'Finished':
            return { kind: 'finished' };
        default:
            // A phase of a newer server: this page cannot show it.
            return waiting;
    }
}
