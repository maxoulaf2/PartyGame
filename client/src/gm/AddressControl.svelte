<script lang="ts">
    import type { GameMasterJoinAddress, GameMasterSnapshot } from '../shared/contracts';
    import type { GameMasterSession } from '../shared/connection/gameMasterSession.svelte';
    import { fr } from '../shared/i18n/fr';

    interface Props {
        snapshot: GameMasterSnapshot;
        session: GameMasterSession;
        /** Whether `snapshot` reflects the server since the connection was last established. */
        fresh: boolean;
    }

    let { snapshot, session, fresh }: Props = $props();

    // The address just chosen, shown until the snapshot advertises it or the server refuses it:
    // the snapshot has the last word.
    let pending = $state<string | null>(null);
    let failed = $state(false);

    // A derived primitive only changes with the address itself, not with every snapshot.
    const advertised = $derived(snapshot.joinAddress);
    const candidates = $derived(snapshot.joinAddressCandidates);
    const canChoose = $derived(session.connected && fresh && pending === null);

    $effect(() => {
        void advertised;
        pending = null;
    });

    function describe(candidate: GameMasterJoinAddress): string {
        const origin = candidate.interfaceName ?? fr.gm.address.configured;
        return fr.gm.address.option
            .replace('{address}', () => candidate.address)
            .replace('{origin}', () => origin);
    }

    const current = $derived.by(() => {
        if (advertised === null) {
            return null;
        }
        const candidate = candidates.find((c) => c.address === advertised);
        return candidate === undefined ? advertised : describe(candidate);
    });

    async function choose(event: Event & { currentTarget: HTMLSelectElement }) {
        const address = event.currentTarget.value;
        if (!canChoose || address === advertised) {
            return;
        }
        failed = false;
        pending = address;
        const outcome = await session.chooseAddress(address);
        if (outcome !== 'chosen') {
            // An unknown address or a lost connection: the snapshot shows what is advertised.
            pending = null;
            failed = outcome === 'ChoiceFailed';
        }
    }
</script>

<section class="address">
    {#if candidates.length > 1}
        <label for="join-address">{fr.gm.address.label}</label>
        <select
            id="join-address"
            value={pending ?? advertised ?? ''}
            disabled={!canChoose}
            aria-describedby="join-address-hint"
            onchange={choose}
        >
            {#if advertised === null}
                <option value="" disabled>{fr.gm.address.none}</option>
            {/if}
            {#each candidates as candidate (candidate.address)}
                <option value={candidate.address}>{describe(candidate)}</option>
            {/each}
        </select>
        <p id="join-address-hint" class="hint">{fr.gm.address.hint}</p>
    {:else}
        <p>
            <span class="label">{fr.gm.address.label}</span>
            <span class="value">{current ?? fr.gm.address.none}</span>
        </p>
    {/if}
    {#if failed}
        <p class="problem" role="alert">{fr.gm.address.failed}</p>
    {/if}
</section>

<style>
    .address {
        display: flex;
        flex-direction: column;
        gap: var(--space-s);
    }

    p {
        margin: 0;
    }

    label,
    .label {
        color: var(--color-text-muted);
    }

    .value {
        font-weight: 700;
        overflow-wrap: anywhere;
    }

    select {
        min-height: var(--touch-target-min);
        padding: 0 var(--space-m);
        border: 2px solid var(--color-text-muted);
        border-radius: var(--radius);
        background: var(--color-bg);
        color: var(--color-text);
        font: inherit;
        /* At least 16 px: Safari on iOS zooms into a smaller field when it gets the focus. */
        font-size: 1.125rem;
        touch-action: manipulation;
    }

    select:focus-visible {
        border-color: var(--color-accent);
        outline: none;
    }

    select:disabled {
        opacity: 0.6;
    }

    .hint {
        color: var(--color-text-muted);
        font-size: 0.875rem;
    }

    .problem {
        color: var(--color-accent);
        font-weight: 700;
    }
</style>
