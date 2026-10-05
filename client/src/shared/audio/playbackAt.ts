import type { AudioPlayback } from '../contracts';

/** What the TV screen must play at a given time. */
export interface PlaybackTarget {
    /** The position in the file, in seconds. */
    readonly position: number;
    /** Whether the excerpt plays at that time. */
    readonly playing: boolean;
    /** Milliseconds until the excerpt starts or ends by itself, or null when it stands still. */
    readonly changesIn: number | null;
}

/**
 * What `playback` plays at `serverNow`, in milliseconds since the Unix epoch on the clock of the
 * server: standing at its position before it starts and while paused, then on with the time since
 * it started, until the end of the excerpt.
 */
export function playbackAt(playback: AudioPlayback, serverNow: number): PlaybackTarget {
    if (playback.startsAt === null) {
        return {
            position: Math.min(playback.position, playback.end),
            playing: false,
            changesIn: null,
        };
    }
    const elapsed = serverNow - playback.startsAt;
    if (elapsed < 0) {
        return { position: playback.position, playing: false, changesIn: -elapsed };
    }
    const position = playback.position + elapsed / 1000;
    return position < playback.end
        ? { position, playing: true, changesIn: (playback.end - position) * 1000 }
        : { position: playback.end, playing: false, changesIn: null };
}
