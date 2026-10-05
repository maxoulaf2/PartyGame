import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import type { AudioPlayback } from '../contracts';
import { ExcerptPlayer, startTimeout } from './excerptPlayer';

/** An audio element that loads nothing: the tests fire its events. */
class FakeAudio extends EventTarget {
    preload = '';
    src = '';
    currentTime = 0;
    paused = true;
    play = vi.fn(() => {
        this.paused = false;
        return Promise.resolve();
    });
    pause = vi.fn(() => {
        this.paused = true;
    });
}

const clock = { serverNow: () => Date.now() };

function playing(url: string): AudioPlayback {
    return { url, position: 0, end: 30, startsAt: Date.now() };
}

describe('ExcerptPlayer', () => {
    let audio: FakeAudio;
    let player: ExcerptPlayer;
    const report = vi.fn();

    beforeEach(() => {
        vi.useFakeTimers();
        report.mockClear();
        audio = new FakeAudio();
        player = new ExcerptPlayer(audio as unknown as HTMLAudioElement);
    });

    afterEach(() => {
        vi.useRealTimers();
    });

    it('reports a file that fires an error, once', () => {
        player.play(playing('/media/a'), clock, report);
        audio.dispatchEvent(new Event('error'));
        audio.dispatchEvent(new Event('error'));
        player.play(playing('/media/a'), clock, report);
        vi.advanceTimersByTime(startTimeout);

        expect(report).toHaveBeenCalledExactlyOnceWith('/media/a');
    });

    it('reports a file that has not started in time', () => {
        player.play(playing('/media/a'), clock, report);
        vi.advanceTimersByTime(startTimeout - 1);
        expect(report).not.toHaveBeenCalled();

        vi.advanceTimersByTime(1);
        expect(report).toHaveBeenCalledExactlyOnceWith('/media/a');
    });

    it('does not report a file that starts in time', () => {
        player.play(playing('/media/a'), clock, report);
        audio.dispatchEvent(new Event('playing'));
        vi.advanceTimersByTime(startTimeout);

        expect(report).not.toHaveBeenCalled();
    });

    it('does not report a file paused before it should start', () => {
        player.play(playing('/media/a'), clock, report);
        player.play({ ...playing('/media/a'), startsAt: null }, clock, report);
        vi.advanceTimersByTime(startTimeout);

        expect(report).not.toHaveBeenCalled();
    });

    it('reports the next file that fails too', () => {
        player.play(playing('/media/a'), clock, report);
        audio.dispatchEvent(new Event('error'));
        player.play(playing('/media/b'), clock, report);
        audio.dispatchEvent(new Event('error'));

        expect(report.mock.calls).toEqual([['/media/a'], ['/media/b']]);
    });
});
