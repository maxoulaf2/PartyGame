/** How long the screen stays white at each whole second of the server, in milliseconds. */
export const flashDuration = 100;

/**
 * Whether the screen is white at `serverTime`, in milliseconds since the Unix epoch: during the
 * first `flashDuration` ms of each whole second, so that devices whose clocks agree flash together.
 */
export function flashLit(serverTime: number): boolean {
    return ((serverTime % 1_000) + 1_000) % 1_000 < flashDuration;
}
