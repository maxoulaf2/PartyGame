import { describe, expect, it } from 'vitest';
import type { AudioPlayback } from '../contracts';
import { playbackAt } from './playbackAt';

const startsAt = 1_800_000_000_000;

// An excerpt from 9.5 s to 24.5 s into its file.
const playing: AudioPlayback = { url: '/media/abc', position: 9.5, end: 24.5, startsAt };
const paused: AudioPlayback = { ...playing, position: 12.25, startsAt: null };

describe('playbackAt', () => {
    it('stands at the start of the excerpt until it starts', () => {
        expect(playbackAt(playing, startsAt - 400)).toEqual({
            position: 9.5,
            playing: false,
            changesIn: 400,
        });
    });

    it('plays on with the time since it started', () => {
        expect(playbackAt(playing, startsAt + 3_000)).toEqual({
            position: 12.5,
            playing: true,
            changesIn: 12_000,
        });
    });

    it('stops at the end of the excerpt', () => {
        expect(playbackAt(playing, startsAt + 15_000)).toEqual({
            position: 24.5,
            playing: false,
            changesIn: null,
        });
        expect(playbackAt(playing, startsAt + 60_000).position).toBe(24.5);
    });

    it('stands where it was paused, whatever the time', () => {
        expect(playbackAt(paused, startsAt + 60_000)).toEqual({
            position: 12.25,
            playing: false,
            changesIn: null,
        });
    });

    it('resumes from where it was paused', () => {
        const resumed = { ...paused, startsAt: startsAt + 10_000 };
        expect(playbackAt(resumed, startsAt + 11_000)).toMatchObject({
            position: 13.25,
            playing: true,
        });
    });
});
