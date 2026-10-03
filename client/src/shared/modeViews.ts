import type { Component } from 'svelte';
import type { ServerClock } from './connection/clockSync.svelte';
import type { IntentOutcome } from './connection/gameHub';
import type {
    DisplayRoundView,
    GameMasterRoundIntent,
    GameMasterRoundView,
    PlayerRoundIntent,
    PlayerRoundView,
    RoundInfo,
} from './contracts';

/**
 * What the player page hands to the player view of a mode: the view of the round in progress for
 * this very player, and the means to act in it.
 */
export interface PlayerViewProps<
    V extends PlayerRoundView = PlayerRoundView,
    I extends PlayerRoundIntent = PlayerRoundIntent,
> {
    readonly view: V;
    readonly round: RoundInfo;
    /** The points of this player since the start of the game, computed by the server alone. */
    readonly score: number;
    /** The clock of the server, to count down to the times of the view. */
    readonly clock: ServerClock;
    /** Whether the page is synchronized with the server: every action is disabled otherwise. */
    readonly interactive: boolean;
    /**
     * Sends an intent to the mode on the server, which alone decides whether it is accepted. Never
     * lost: an intent the connection drops is sent again once it is back, even after a reload.
     */
    readonly send: (intent: I) => void;
    /**
     * The intents of this round sent and not acknowledged yet, oldest first, to show at once as
     * pending. Once acknowledged, the snapshot tells what became of them.
     */
    readonly pending: readonly I[];
}

/** What the TV page hands to the display view of a mode. The TV screen never acts. */
export interface DisplayViewProps<V extends DisplayRoundView = DisplayRoundView> {
    readonly view: V;
    readonly round: RoundInfo;
    /** The clock of the server, to count down to the times of the view. */
    readonly clock: ServerClock;
}

/**
 * What the GM console hands to the game master view of a mode: the view of the round in progress,
 * secrets included, and the means to drive it.
 */
export interface GameMasterViewProps<
    V extends GameMasterRoundView = GameMasterRoundView,
    I extends GameMasterRoundIntent = GameMasterRoundIntent,
> {
    readonly view: V;
    readonly round: RoundInfo;
    /** The clock of the server, to count down to the times of the view. */
    readonly clock: ServerClock;
    /** Whether the console is synchronized with the server: every action is disabled otherwise. */
    readonly interactive: boolean;
    /** Sends an intent to the mode on the server, which alone decides whether it is accepted. */
    readonly send: (intent: I) => Promise<IntentOutcome>;
}

/** The views of a role whose `type` names the mode `T`. */
type ViewOfMode<U, T extends string> = Extract<U, { readonly type: T }>;

/** The intents of a role whose `type` is the one of the mode `T`, a dot, then their name. */
type IntentOfMode<U, T extends string> = Extract<U, { readonly type: `${T}.${string}` }>;

/** The three views of the mode whose rounds have the type `T`, one per role. */
export interface ModeViews<T extends string> {
    readonly player: Component<
        PlayerViewProps<ViewOfMode<PlayerRoundView, T>, IntentOfMode<PlayerRoundIntent, T>>
    >;
    readonly display: Component<DisplayViewProps<ViewOfMode<DisplayRoundView, T>>>;
    readonly gm: Component<
        GameMasterViewProps<
            ViewOfMode<GameMasterRoundView, T>,
            IntentOfMode<GameMasterRoundIntent, T>
        >
    >;
}
