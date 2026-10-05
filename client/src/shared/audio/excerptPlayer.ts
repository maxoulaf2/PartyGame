import type { ServerClock } from '../connection/clockSync.svelte';
import type { AudioPlayback } from '../contracts';
import { playbackAt } from './playbackAt';

/** How far the element may drift from where it should be before it seeks, in seconds. */
const tolerance = 0.05;

/**
 * Plays on the TV screen the excerpt its snapshot describes, with a single `<audio>` element: it
 * streams the file with range requests, from the position asked, so that an excerpt from the
 * middle of a track downloads only what it plays.
 *
 * Told the playback at every snapshot and every clock synchronization, it brings the element to
 * where it should be, then starts and stops it by itself on time.
 */
export class ExcerptPlayer {
    readonly #audio: HTMLAudioElement;
    #playback: AudioPlayback | null = null;
    #clock: Pick<ServerClock, 'serverNow'> | null = null;
    #url: string | null = null;
    #timer: ReturnType<typeof setTimeout> | undefined;

    constructor(audio: HTMLAudioElement) {
        this.#audio = audio;
        audio.preload = 'auto';
        // A file not ready on time starts late: once it plays, it jumps to where it should be.
        audio.addEventListener('playing', () => this.#sync());
    }

    /** Plays what `playback` describes, from now on, on the clock of the server. */
    play(playback: AudioPlayback, clock: Pick<ServerClock, 'serverNow'>): void {
        this.#playback = playback;
        this.#clock = clock;
        this.#sync();
    }

    /** Stops, for instance once the round is over. */
    stop(): void {
        this.#playback = null;
        this.#sync();
    }

    #sync(): void {
        clearTimeout(this.#timer);
        const audio = this.#audio;
        if (this.#playback === null || this.#clock === null) {
            audio.pause();
            return;
        }
        if (this.#url !== this.#playback.url) {
            this.#url = this.#playback.url;
            audio.src = this.#url;
        }
        const target = playbackAt(this.#playback, this.#clock.serverNow());
        if (Math.abs(audio.currentTime - target.position) > tolerance) {
            audio.currentTime = target.position;
        }
        if (!target.playing) {
            audio.pause();
        } else if (audio.paused) {
            // Refused while the audio is locked: the click on « Démarrer » plays it, from where it
            // should be by then. Interrupted by a pause, it needs nothing. A file that cannot be
            // read is US-E14-04.
            audio.play().catch((error: unknown) => {
                if (error instanceof DOMException && error.name === 'NotAllowedError') {
                    document.addEventListener('click', () => this.#sync(), {
                        once: true,
                        capture: true,
                    });
                }
            });
        }
        if (target.changesIn !== null) {
            this.#timer = setTimeout(() => this.#sync(), target.changesIn);
        }
    }
}

let player: ExcerptPlayer | null = null;

/**
 * The player of the TV screen, created on first use: the phones, whose pages bundle the views of
 * the TV screen too, never create one.
 */
export function excerptPlayer(): ExcerptPlayer {
    if (player === null) {
        const audio = document.createElement('audio');
        // In the page, hidden, for the tests to read its state.
        audio.hidden = true;
        document.body.append(audio);
        player = new ExcerptPlayer(audio);
    }
    return player;
}
