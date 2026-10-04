<script lang="ts">
    import QrCode from '../shared/components/QrCode.svelte';
    import type { NetworkHealth } from '../shared/contracts';
    import { composeJoinUrl } from '../shared/connection/joinUrl';
    import { fr } from '../shared/i18n/fr';
    import { diagnosticText, diagnosticUrl } from './networkText';

    interface Props {
        /** The address advertised to phones, or null when the server knows none. */
        joinAddress: string | null;
        /** The diagnostics run so far, null until the server sends them. */
        network: NetworkHealth | null;
        /** Open in the lobby, when the game master checks the venue; closed once playing. */
        open: boolean;
    }

    let { joinAddress, network, open }: Props = $props();

    const url = $derived(
        joinAddress === null ? null : diagnosticUrl(composeJoinUrl(joinAddress, location)),
    );
    const diagnostics = $derived(network?.diagnostics ?? []);
</script>

<details {open}>
    <summary>{fr.gm.network.title}</summary>
    <div class="content">
        <p class="hint">{fr.gm.network.hint}</p>
        {#if url === null}
            <p>{fr.gm.network.none}</p>
        {:else}
            <p class="url">{url}</p>
            <div class="qr">
                <QrCode text={url} label={fr.gm.network.qrLabel} />
            </div>
        {/if}
        {#if diagnostics.length === 0}
            <p class="hint">{fr.gm.network.noDiagnostic}</p>
        {:else}
            <ul aria-label={fr.gm.network.diagnosticsLabel}>
                {#each diagnostics as diagnostic, i (i)}
                    <li class={diagnostic.verdict}>{diagnosticText(diagnostic)}</li>
                {/each}
            </ul>
        {/if}
    </div>
</details>

<style>
    details {
        border-radius: var(--radius);
        background: var(--color-surface);
    }

    summary {
        display: flex;
        align-items: center;
        min-height: var(--touch-target-min);
        padding: 0 var(--space-m);
        font-weight: 700;
        cursor: pointer;
        touch-action: manipulation;
    }

    .content {
        display: flex;
        flex-direction: column;
        gap: var(--space-s);
        padding: 0 var(--space-m) var(--space-m);
    }

    p,
    ul {
        margin: 0;
    }

    .hint {
        color: var(--color-text-muted);
        font-size: 0.875rem;
    }

    .url {
        font-weight: 700;
        overflow-wrap: anywhere;
    }

    .qr {
        width: min(12rem, 100%);
    }

    ul {
        display: flex;
        flex-direction: column;
        gap: 0.25rem;
        padding: 0;
        list-style: none;
    }

    /* The verdict is written in the text: the border only repeats it. */
    li {
        padding-left: var(--space-s);
        border-left: 3px solid var(--choice-d-color);
    }

    li.Reserved {
        border-left-color: var(--color-accent);
    }

    li.Problem {
        border-left-color: var(--choice-a-color);
    }
</style>
