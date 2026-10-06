import { cubicOut } from 'svelte/easing';
import type { TransitionConfig } from 'svelte/transition';

/** Under the 600 ms past which a change of screen drags. */
export const screenChangeMilliseconds = 450;

/**
 * The TV screen going out or coming in, both at once: a fade, with a slight zoom unless the viewer
 * asks for less motion. Never larger than its natural size, so that nothing crosses into the edges
 * TVs may crop. Opacity and transform only, which the browser animates without layout, even on a
 * Raspberry Pi.
 */
function screenChange(): TransitionConfig {
    const still = matchMedia('(prefers-reduced-motion: reduce)').matches;
    return {
        duration: screenChangeMilliseconds,
        easing: cubicOut,
        // `t` goes from 0 to 1 coming in, from 1 to 0 going out: in from slightly smaller, out
        // receding.
        css: (t) =>
            still ? `opacity: ${t}` : `opacity: ${t}; transform: scale(${0.96 + 0.04 * t})`,
    };
}

/** The TV screen coming in. */
export const screenIn: (node: Element) => TransitionConfig = screenChange;

/**
 * The TV screen going out, no longer part of the page: out of reach of a click and of assistive
 * technologies.
 */
export function screenOut(node: Element): TransitionConfig {
    node.setAttribute('inert', '');
    node.setAttribute('aria-hidden', 'true');
    return screenChange();
}
