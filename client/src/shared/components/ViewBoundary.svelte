<script lang="ts">
    import type { Snippet } from 'svelte';
    import { reportError } from '../errors/errorReporting';
    import { BoundaryRetry } from './boundaryRetry';

    interface Props {
        /**
         * What the page shows, such as its snapshot: once it changes, a view that failed is tried
         * again, so that a passing error clears by itself.
         */
        shown: unknown;
        /** Shown instead of the view while it fails: never a technical message. */
        fallback: Snippet;
        /** The view to protect. */
        children: Snippet;
    }

    let { shown, fallback, children }: Props = $props();

    const retry = new BoundaryRetry();

    $effect(() => {
        retry.show(shown);
    });

    // The errors of the event handlers and of asynchronous code never reach a boundary: the
    // global handlers of the page report them.
    function onerror(error: unknown, reset: () => void) {
        reportError('RenderFailed', error);
        retry.failed(shown, reset);
    }
</script>

<svelte:boundary {onerror}>
    {@render children()}

    {#snippet failed()}
        {@render fallback()}
    {/snippet}
</svelte:boundary>
