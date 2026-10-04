<script lang="ts">
    import type { ConnectionQuality } from '../shared/contracts';
    import { fr } from '../shared/i18n/fr';
    import { qualityParts } from './networkText';

    interface Props {
        quality: ConnectionQuality;
    }

    let { quality }: Props = $props();
</script>

<span class="quality">
    {#each qualityParts(quality) as part, i (i)}
        {#if i > 0}<span aria-hidden="true"> · </span>{/if}
        <span class:poor={part.poor}>
            {#if part.poor}<span
                    title={fr.gm.network.warningLabel}
                    aria-label={fr.gm.network.warningLabel}>{fr.gm.network.warning}</span
                >{/if}{part.text}
        </span>
    {/each}
</span>

<style>
    .quality {
        color: var(--color-text-muted);
        font-size: 0.875rem;
    }

    .poor {
        color: var(--color-accent);
        font-weight: 700;
    }
</style>
