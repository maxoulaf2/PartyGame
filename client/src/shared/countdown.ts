/**
 * The whole seconds left before `closeAt`, at `now`, both times of the server in milliseconds
 * since the Unix epoch. Rounded up, so that every screen shows the same number at the same time,
 * and 0 once reached: the countdown never decides anything, the snapshot does.
 */
export function secondsLeft(closeAt: number, now: number): number {
    return Math.max(0, Math.ceil((closeAt - now) / 1000));
}

/**
 * The milliseconds before the countdown to `closeAt` shows its next number, at `now`: the
 * screens all change their number when a whole second of the server passes. Null once it shows 0.
 */
export function untilNextSecond(closeAt: number, now: number): number | null {
    const left = closeAt - now;
    if (left <= 0) {
        return null;
    }
    const remainder = left % 1000;
    return remainder === 0 ? 1000 : remainder;
}
