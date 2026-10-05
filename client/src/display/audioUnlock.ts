// A tenth of a second of silence, in the build: unlocking the audio requests nothing outside.
import silence from './silence.wav';

/**
 * Whether the browser lets the page play sound, found by playing a short silence: it refuses
 * unless a click allowed it, or it plays sound without one (Chromium in kiosk mode with
 * `--autoplay-policy=no-user-gesture-required`). Called on a click, it unlocks the audio.
 */
export async function unlockAudio(): Promise<boolean> {
    try {
        await new Audio(silence).play();
        return true;
    } catch {
        return false;
    }
}
