import type { Component } from 'svelte';
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
    /** Whether the page is synchronized with the server: every action is disabled otherwise. */
    readonly interactive: boolean;
    /** Sends an intent to the mode on the server, which alone decides whether it is accepted. */
    readonly send: (intent: I) => Promise<IntentOutcome>;
}

/** What the TV page hands to the display view of a mode. The TV screen never acts. */
export interface DisplayViewProps<V extends DisplayRoundView = DisplayRoundView> {
    readonly view: V;
    readonly round: RoundInfo;
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
    /** Whether the console is synchronized with the server: every action is disabled otherwise. */
    readonly interactive: boolean;
    /** Sends an intent to the mode on the server, which alone decides whether it is accepted. */
    readonly send: (intent: I) => Promise<IntentOutcome>;
}

/** The views and intents of a role whose `type` names the mode `T`. */
type OfMode<U, T extends string> = Extract<U, { readonly type: T }>;

/** The three views of the mode whose rounds have the type `T`, one per role. */
export interface ModeViews<T extends string> {
    readonly player: Component<
        PlayerViewProps<OfMode<PlayerRoundView, T>, OfMode<PlayerRoundIntent, T>>
    >;
    readonly display: Component<DisplayViewProps<OfMode<DisplayRoundView, T>>>;
    readonly gm: Component<
        GameMasterViewProps<OfMode<GameMasterRoundView, T>, OfMode<GameMasterRoundIntent, T>>
    >;
}
