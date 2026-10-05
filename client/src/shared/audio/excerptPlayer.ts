import type { ServerClock } from '../connection/clockSync.svelte';
import type { AudioPlayback } from '../contracts';
import { playbackAt } from './playbackAt';

/** How far the element may drift from where it should be before it seeks, in seconds. */
const tolerance = 0.05;

/** How long an excerpt may take to start once it should play before the file counts as failed. */
export const startTimeout = 3_000;

/**
 * Plays on the TV screen the excerpt its snapshot describes, with a single `<audio>` element: it
 * streams the file with range requests, from the position asked, so that an excerpt from the
 * middle of a track downloads only what it plays.
 *
 * Told the playback at every snapshot and every clock synchronization, it brings the element to
 * where it should be, then starts and stops it by itself on time. A file it cannot load, or that
 * has not started {@link startTimeout} ms after it should, is reported once, and nothing shows.
 */
export class ExcerptPlayer {
    readonly #audio: HTMLAudioElement;
    #playback: AudioPlayback | null = null;
    #clock: Pick<ServerClock, 'serverNow'> | null = null;
    #url: string | null = null;
    #timer: ReturnType<typeof setTimeout> | undefined;
    #report: (url: string) => void = () => {};
    /** Armed when the element is asked to play, until it does. */
    #watchdog: ReturnType<typeof setTimeout> | undefined;
    #locked = false;
    #reported = false;

    constructor(audio: HTMLAudioElement) {
        this.#audio = audio;
        audio.preload = 'auto';
        // A file not ready on time starts late: once it plays, it jumps to where it should be.
        audio.addEventListener('playing', () => {
            this.#disarm();
            this.#sync();
        });
        audio.addEventListener('error', () => this.#fail());
    }

    /**
     * Plays what `playback` describes, from now on, on the clock of the server, and tells `report`
     * of a file that cannot be played.
     */
    play(
        playback: AudioPlayback,
        clock: Pick<ServerClock, 'serverNow'>,
        report: (url: string) => void = () => {},
    ): void {
        this.#playback = playback;
        this.#clock = clock;
        this.#report = report;
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
            this.#disarm();
            audio.pause();
            return;
        }
        if (this.#url !== this.#playback.url) {
            this.#disarm();
            this.#url = this.#playback.url;
            this.#reported = false;
            audio.src = this.#url;
        }
        const target = playbackAt(this.#playback, this.#clock.serverNow());
        if (Math.abs(audio.currentTime - target.position) > tolerance) {
            audio.currentTime = target.position;
        }
        if (!target.playing) {
            this.#disarm();
            audio.pause();
        } else if (audio.paused) {
            this.#watchdog ??= setTimeout(() => {
                this.#watchdog = undefined;
                // A screen whose audio is locked is no failed file: the console warns of it already.
                if (!this.#locked) {
                    this.#fail();
                }
            }, startTimeout);
            // Refused while the audio is locked: the click on « Démarrer » plays it, from where it
            // should be by then. Interrupted by a pause, it needs nothing. A file that cannot be
            // read fires an error, or never plays.
            audio.play().then(
                () => (this.#locked = false),
                (error: unknown) => {
                    if (error instanceof DOMException && error.name === 'NotAllowedError') {
                        this.#locked = true;
                        document.addEventListener(
                            'click',
                            () => {
                                this.#locked = false;
                                this.#sync();
                            },
                            { once: true, capture: true },
                        );
                    }
                },
            );
        }
        if (target.changesIn !== null) {
            this.#timer = setTimeout(() => this.#sync(), target.changesIn);
        }
    }

    #disarm(): void {
        clearTimeout(this.#watchdog);
        this.#watchdog = undefined;
    }

    /** Reports the file once: it keeps failing the same way until the next one. */
    #fail(): void {
        this.#disarm();
        if (this.#url !== null && !this.#reported) {
            this.#reported = true;
            this.#report(this.#url);
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
