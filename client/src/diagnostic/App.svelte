<script lang="ts">
    import { onMount } from 'svelte';
    import ViewBoundary from '../shared/components/ViewBoundary.svelte';
    import WaitingScreen from '../shared/components/WaitingScreen.svelte';
    import type { PlayerSnapshot } from '../shared/contracts';
    import { watchBuild } from '../shared/connection/buildCheck';
    import { createGameConnection } from '../shared/connection/gameHub';
    import { SnapshotStore } from '../shared/connection/snapshotStore.svelte';
    import { connectErrorReporting } from '../shared/errors/errorReporting';
    import { fill } from '../shared/i18n/fill';
    import { fr } from '../shared/i18n/fr';
    import { assessDiagnostic, type DiagnosticMeasures } from '../shared/networkQuality';
    import {
        createProbe,
        measureNetwork,
        stabilitySeconds,
        type DiagnosticStep,
    } from './measureNetwork';
    import { describeMeasures } from './measuresText';

    // The page never registers: it reaches the hub anonymously, like a phone before it joins.
    const connection = createGameConnection();
    const probe = createProbe(connection);

    let step = $state<DiagnosticStep | null>(null);
    let measures = $state.raw<DiagnosticMeasures | null>(null);
    const outcome = $derived(measures && assessDiagnostic(measures));

    const stepTexts: Readonly<Record<DiagnosticStep, string>> = {
        ...fr.diagnostic.steps,
        stability: fill(fr.diagnostic.steps.stability, { seconds: stabilitySeconds }),
    };

    async function run() {
        measures = null;
        const result = await measureNetwork(probe, (current) => (step = current));
        step = null;
        measures = result;
        if (result.transport !== null) {
            // For the game master console; dropped if the connection is lost meanwhile.
            const median = result.roundTrips?.median;
            connection
                .invoke('ReportNetworkDiagnostic', {
                    verdict: assessDiagnostic(result).verdict,
                    roundTripMedian: median === undefined ? null : Math.round(median),
                })
                .catch(() => {});
        }
    }

    onMount(() => {
        // Before the connection starts, so as not to miss the welcome.
        const stopBuild = watchBuild(connection);
        // No snapshot to tell about: the page shows none.
        const stopErrors = connectErrorReporting(connection, new SnapshotStore<PlayerSnapshot>());
        connection.start().catch(() => {});
        void run();
        return () => {
            stopBuild();
            stopErrors();
            connection.stop().catch(() => {});
        };
    });
</script>

{#snippet unavailable()}
    <WaitingScreen title={fr.diagnostic.title} message={fr.diagnostic.intro} />
{/snippet}

<ViewBoundary shown={measures} fallback={unavailable}>
    <main>
        <h1>{fr.diagnostic.title}</h1>
        {#if measures === null || outcome === null}
            <p>{fr.diagnostic.intro}</p>
            <p class="step" role="status">{step === null ? '' : stepTexts[step]}</p>
        {:else}
            <p class="verdict {outcome.verdict}" role="status">
                {fr.diagnostic.verdicts[outcome.verdict]}
            </p>
            {#if outcome.findings.length > 0}
                <ul>
                    {#each outcome.findings as finding (finding)}
                        <li>{fr.diagnostic.advice[finding]}</li>
                    {/each}
                </ul>
            {/if}
            <dl aria-label={fr.diagnostic.measuresLabel}>
                {#each describeMeasures(measures) as line (line.label)}
                    <div>
                        <dt>{line.label}</dt>
                        <dd>{line.value}</dd>
                    </div>
                {/each}
            </dl>
            <button type="button" onclick={run}>{fr.diagnostic.retry}</button>
        {/if}
    </main>
</ViewBoundary>

<style>
    main {
        display: flex;
        flex-direction: column;
        gap: var(--space-m);
        max-width: 32rem;
        margin: 0 auto;
        padding: var(--space-l) var(--space-m);
    }

    h1,
    p,
    ul,
    dl,
    dd {
        margin: 0;
    }

    h1 {
        color: var(--color-accent);
        font-size: var(--font-size-title);
    }

    .step {
        min-height: 1.4em;
        color: var(--color-text-muted);
    }

    .verdict {
        font-size: 1.5rem;
        font-weight: 700;
    }

    /* The symbol of the text tells the verdict too: the color is never alone. */
    .verdict.Good {
        color: var(--choice-d-color);
    }

    .verdict.Reserved {
        color: var(--color-accent);
    }

    .verdict.Problem {
        color: var(--choice-a-color);
    }

    ul {
        display: flex;
        flex-direction: column;
        gap: var(--space-s);
        padding-left: 1.25rem;
    }

    dl {
        display: flex;
        flex-direction: column;
        gap: var(--space-s);
        padding: var(--space-m);
        border-radius: var(--radius);
        background: var(--color-surface);
    }

    dt {
        color: var(--color-text-muted);
        font-size: 0.875rem;
    }

    dd {
        font-weight: 700;
        overflow-wrap: anywhere;
    }

    button {
        min-height: var(--touch-target-min);
        padding: 0 var(--space-m);
        border: none;
        border-radius: var(--radius);
        background: var(--color-accent);
        color: var(--color-bg);
        font: inherit;
        font-weight: 700;
        cursor: pointer;
        touch-action: manipulation;
    }
</style>
