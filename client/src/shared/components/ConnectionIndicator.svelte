<script lang="ts">
    import type { ConnectionStatus } from '../connection/connectionStatus.svelte';
    import { fr } from '../i18n/fr';
    import ConnectionIcon from './ConnectionIcon.svelte';

    interface Props {
        status: ConnectionStatus;
        /** Sized and placed for a TV seen from across the room, inside the area TVs may crop. */
        tv?: boolean;
    }

    let { status, tv = false }: Props = $props();
</script>

<!-- Always in the page, so that screen readers announce the notice when it appears. Fixed and
     untouchable: it neither shifts nor blocks the content. -->
<div class="indicator" class:tv role="status">
    {#if status.state === 'reconnecting'}
        <span class="notice">
            <ConnectionIcon connected={false} />
            {fr.connection.reconnecting}
        </span>
    {/if}
</div>

<style>
    .indicator {
        position: fixed;
        top: calc(env(safe-area-inset-top) + var(--space-s) / 2);
        left: 50%;
        z-index: 1;
        transform: translateX(-50%);
        pointer-events: none;
    }

    /* TVs may crop their edges (overscan): kept within the 5% margin of the TV screen layout. */
    .indicator.tv {
        top: 5vh;
        right: 5vw;
        left: auto;
        transform: none;
        font-size: 0.5em;
    }

    .notice {
        display: inline-flex;
        align-items: center;
        gap: 0.4em;
        padding: 0.25em 0.75em;
        border: var(--sticker-line, 0) solid var(--color-ink);
        border-radius: 999px;
        background: var(--color-surface);
        color: var(--color-on-surface-muted);
        font-size: 0.875rem;
        white-space: nowrap;
    }

    .tv .notice {
        font-size: 1em;
    }
</style>
