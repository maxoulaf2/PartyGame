/**
 * When a view that failed to render is tried again: once the page shows something else than what
 * it failed with, such as a newer snapshot. A passing error then clears by itself, while a view
 * that fails with every snapshot is tried once per snapshot, never in a loop.
 */
export class BoundaryRetry {
    #failure: { readonly shown: unknown; readonly reset: () => void } | null = null;

    /** Records that the view failed while the page showed `shown`, and how to try it again. */
    failed(shown: unknown, reset: () => void): void {
        this.#failure = { shown, reset };
    }

    /** Tells that the page now shows `shown`: tries the failed view again if that changed. */
    show(shown: unknown): void {
        const failure = this.#failure;
        if (failure !== null && failure.shown !== shown) {
            this.#failure = null;
            failure.reset();
        }
    }
}
