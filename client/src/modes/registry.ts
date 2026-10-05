import type { Component } from 'svelte';
import type { DisplayRoundView, GameMasterRoundView, PlayerRoundView } from '../shared/contracts';
import type {
    DisplayViewProps,
    GameMasterViewProps,
    ModeViews,
    PlayerViewProps,
} from '../shared/modeViews';
import BlindTestDisplay from './blindtest/BlindTestDisplay.svelte';
import BlindTestGameMaster from './blindtest/BlindTestGameMaster.svelte';
import BlindTestPlayer from './blindtest/BlindTestPlayer.svelte';
import BuzzerDisplay from './buzzer/BuzzerDisplay.svelte';
import BuzzerGameMaster from './buzzer/BuzzerGameMaster.svelte';
import BuzzerPlayer from './buzzer/BuzzerPlayer.svelte';
import QuizDisplay from './quiz/QuizDisplay.svelte';
import QuizGameMaster from './quiz/QuizGameMaster.svelte';
import QuizPlayer from './quiz/QuizPlayer.svelte';

/** Every type of round view the server may send, whatever the role. */
type RoundViewType =
    PlayerRoundView['type'] | DisplayRoundView['type'] | GameMasterRoundView['type'];

/**
 * The views of every game mode, by the type of its rounds: adding a mode means adding its line.
 * Checked against the generated contracts: a type of round view without its line, or views that do
 * not take the views of their type, fail `npm run check`.
 */
export const modes = {
    quiz: { player: QuizPlayer, display: QuizDisplay, gm: QuizGameMaster },
    buzzer: { player: BuzzerPlayer, display: BuzzerDisplay, gm: BuzzerGameMaster },
    blindtest: { player: BlindTestPlayer, display: BlindTestDisplay, gm: BlindTestGameMaster },
} satisfies { readonly [T in RoundViewType]: ModeViews<T> };

/** The views of a mode, as the pages use them, whatever the type of the round. */
interface AnyModeViews {
    readonly player: Component<PlayerViewProps>;
    readonly display: Component<DisplayViewProps>;
    readonly gm: Component<GameMasterViewProps>;
}

// Each type is paired with the views of that very type, so a view always reaches a component that
// expects it: a correlation the compiler cannot follow through a lookup by type.
const byType = modes as Readonly<Partial<Record<string, AnyModeViews>>>;

function modeOf(view: { readonly type: string }): AnyModeViews | null {
    // Own keys only: a type such as « toString » is an unknown mode, not a method of the object.
    return Object.hasOwn(byType, view.type) ? (byType[view.type] ?? null) : null;
}

/** The player view of the mode of `view`, or null for a mode this build does not know. */
export function findPlayerView(view: PlayerRoundView): Component<PlayerViewProps> | null {
    return modeOf(view)?.player ?? null;
}

/** The TV view of the mode of `view`, or null for a mode this build does not know. */
export function findDisplayView(view: DisplayRoundView): Component<DisplayViewProps> | null {
    return modeOf(view)?.display ?? null;
}

/** The game master view of the mode of `view`, or null for a mode this build does not know. */
export function findGameMasterView(
    view: GameMasterRoundView,
): Component<GameMasterViewProps> | null {
    return modeOf(view)?.gm ?? null;
}
